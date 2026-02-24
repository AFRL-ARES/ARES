using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using AresScript.Interpreters;
using System.Text;
using System.Text.RegularExpressions;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  private static async Task BuildEnvironmentForCompletions(AresScriptEnvironment environment, string script, int line)
  {
    if(string.IsNullOrWhiteSpace(script))
    {
      return;
    }

    try
    {
      var normalizedScript = script.EndsWith('\n') ? script : script + "\n";
      var stream = new AntlrInputStream(normalizedScript);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      var programCtx = parser.program();
      var validator = new AresValidationInterpreter(environment, AresValidationInterpreter.ValidationMode.Lenient, line);
      await validator.Visit(programCtx);
    }
    catch
    {
      // Ignore parse/validation errors for autocomplete; fall back to system scope.
    }
  }

  public static async Task<IReadOnlyList<CompletionItem>> BuildCompletionsAsync(
    AresScriptEnvironment environment,
    string script,
    int cursorLine,
    int cursorColumn)
  {
    await BuildEnvironmentForCompletions(environment, script, cursorLine);
    var systemVariables = environment.GetAllSystemVariables();
    var userFunctions = environment.GetAllUserFunctions();
    var userVariables = environment.GetAllUserVariables();
    var items = new List<CompletionItem>();

    if(TryGetParentIdentifier(script, cursorLine, cursorColumn, out var parentIdentifier))
    {
      if(environment.TryGetSystemValue(parentIdentifier, out var systemParent)
        && systemParent.Kind == AresSystemValue.AresSystemValueKind.Struct
        && systemParent.StructFields is not null)
      {
        foreach(var (key, fieldValue) in systemParent.StructFields)
        {
          AddCompletionForSystemValue(items, environment, key, fieldValue, parentIdentifier);
        }

        AddExtensionCompletions(items, environment, systemParent.ToAresValue(), parentIdentifier);
      }
      else if(environment.TryGetValue(parentIdentifier, out var parentValue)
        && parentValue.KindCase == AresValue.KindOneofCase.StructValue
        && parentValue.StructValue is not null)
      {
        foreach(var field in parentValue.StructValue.Fields)
        {
          AddCompletionForAresValue(items, environment, field.Key, field.Value, parentIdentifier);
        }

        AddExtensionCompletions(items, environment, parentValue, parentIdentifier);
      }
      else if(environment.TryGetValue(parentIdentifier, out var plainValue))
      {
        AddExtensionCompletions(items, environment, plainValue, parentIdentifier);
      }
    }
    else
    {
      foreach(var (key, value) in systemVariables)
      {
        AddCompletionForSystemValue(items, environment, key, value, string.Empty);
      }

      items.AddRange(userFunctions.Select(func => new CompletionItem
      {
        Label = func.Name,
        InsertText = func.Name,
        Detail = "User function",
        Kind = SymbolKind.Function
      }));

      items.AddRange(userVariables.Select(variable => new CompletionItem
      {
        Label = variable.Key,
        InsertText = variable.Key,
        Detail = "User variable",
        Kind = SymbolKind.Variable,
        Schema = variable.Value.ToSchemaEntry()
      }));
    }

    if(IsTypeHintContext(script, cursorLine, cursorColumn))
    {
      AddTypeHintCompletions(items);
    }

    return items;
  }

  public static bool TryGetParentIdentifier(string script, int cursorLine, int cursorColumn, out string parentIdentifier)
  {
    parentIdentifier = string.Empty;

    if(cursorLine <= 0 || cursorColumn <= 0)
    {
      return false;
    }

    if(string.IsNullOrEmpty(script))
    {
      return false;
    }

    var lines = script.Split(["\r\n", "\n"], StringSplitOptions.None);
    if(cursorLine > lines.Length)
    {
      return false;
    }

    var line = lines[cursorLine - 1];
    var safeColumn = Math.Min(cursorColumn - 1, line.Length);
    var prefix = line[..safeColumn];

    var dotIndex = prefix.LastIndexOf('.');
    if(dotIndex < 0)
    {
      return false;
    }

    // ex.: don't get "my_furnace" in "my_furnace.get_temp(| "
    var lastOpenParen = prefix.LastIndexOf('(');
    var lastCloseParen = prefix.LastIndexOf(')');
    if(lastOpenParen > dotIndex && lastOpenParen > lastCloseParen)
    {
      return false;
    }

    var left = prefix[..dotIndex];
    var identifier = ExtractTrailingIdentifier(left);
    if(string.IsNullOrEmpty(identifier))
    {
      return false;
    }

    parentIdentifier = identifier;
    return true;
  }

  private static string ExtractTrailingIdentifier(string text)
  {
    var match = IdentifierRegex().Match(text);
    return match.Success ? match.Groups[1].Value : string.Empty;
  }

  private static string BuildSnippet(string funcName, AresDataSchema schema)
  {
    var builder = new StringBuilder();
    builder.Append(funcName);
    builder.Append('(');
    var requiredFields = schema.Fields.Where(field => !field.Value.Optional).ToList();
    for(var i = 0; i < requiredFields.Count; i++)
    {
      var fieldElement = requiredFields[i];
      builder.Append($"${{{i + 1}:{fieldElement.Key}}}");
      if(i < requiredFields.Count - 1)
      {
        builder.Append(',');
      }
    }
    builder.Append(')');
    return builder.ToString();
  }

  private static void AddExtensionCompletions(
    ICollection<CompletionItem> items,
    AresScriptEnvironment environment,
    AresValue parentValue,
    string parentIdentifier)
  {
    var extensionFunctions = environment.GetExtensionFunctions(parentValue);
    foreach(var extensionFunc in extensionFunctions)
    {
      var schemaForCall = TrimReceiverFromSchema(extensionFunc.InputSchema);
      items.Add(new CompletionItem
      {
        Label = extensionFunc.Name,
        InsertText = BuildSnippet(extensionFunc.Name, schemaForCall),
        Detail = extensionFunc.Description,
        Documentation = extensionFunc.Description,
        Kind = SymbolKind.Function,
        ParentIdentifier = parentIdentifier,
        InputSchema = schemaForCall,
        OutputSchema = extensionFunc.OutputSchema
      });
    }
  }

  // First argument is always 'self' so we don't need that for validation
  private static AresDataSchema TrimReceiverFromSchema(AresDataSchema schema)
  {
    if(schema.Fields.Count <= 1)
    {
      return new AresDataSchema();
    }

    var trimmed = new AresDataSchema();
    foreach(var (name, entry) in schema.Fields.Skip(1))
    {
      trimmed.Fields[name] = entry;
    }

    return trimmed;
  }

  private static void AddCompletionForSystemValue(
    ICollection<CompletionItem> items,
    AresScriptEnvironment environment,
    string label,
    AresSystemValue value,
    string parentIdentifier)
  {
    if(value.RawValue?.FunctionValue is not null
      && environment.TryGetSystemFunction(value.RawValue.FunctionValue.FunctionId, out var systemFunction))
    {
      var description = string.IsNullOrWhiteSpace(value.Description)
        ? systemFunction.Description
        : value.Description;
      items.Add(new CompletionItem
      {
        Label = label,
        InsertText = BuildSnippet(label, systemFunction.InputSchema),
        Detail = description,
        Documentation = description,
        Kind = SymbolKind.Function,
        ParentIdentifier = parentIdentifier,
        InputSchema = systemFunction.InputSchema,
        OutputSchema = systemFunction.OutputSchema
      });
      return;
    }

    var schemaValue = value.ToAresValue();
    items.Add(new CompletionItem
    {
      Label = label,
      InsertText = label,
      Detail = value.Description ?? string.Empty,
      Documentation = value.Description ?? string.Empty,
      Kind = value.Kind == AresSystemValue.AresSystemValueKind.Struct
        ? SymbolKind.Struct
        : SymbolKind.Variable,
      ParentIdentifier = parentIdentifier,
      Schema = schemaValue.ToSchemaEntry()
    });
  }

  private static void AddCompletionForAresValue(
    ICollection<CompletionItem> items,
    AresScriptEnvironment environment,
    string label,
    AresValue value,
    string parentIdentifier)
  {
    if(value.FunctionValue is not null
      && environment.TryGetSystemFunction(value.FunctionValue.FunctionId, out var systemFunction))
    {
      items.Add(new CompletionItem
      {
        Label = label,
        InsertText = BuildSnippet(label, systemFunction.InputSchema),
        Detail = systemFunction.Description,
        Documentation = systemFunction.Description,
        Kind = SymbolKind.Function,
        ParentIdentifier = parentIdentifier,
        InputSchema = systemFunction.InputSchema,
        OutputSchema = systemFunction.OutputSchema
      });
      return;
    }

    items.Add(new CompletionItem
    {
      Label = label,
      InsertText = label,
      Detail = string.Empty,
      Documentation = string.Empty,
      Kind = value.KindCase == AresValue.KindOneofCase.StructValue
        ? SymbolKind.Struct
        : SymbolKind.Variable,
      ParentIdentifier = parentIdentifier,
      Schema = value.ToSchemaEntry()
    });
  }

  private static void AddTypeHintCompletions(ICollection<CompletionItem> items)
  {
    foreach(var typeName in AresScriptTypeHints.AvailableTypeNames)
    {
      items.Add(new CompletionItem
      {
        Label = typeName,
        InsertText = typeName,
        Detail = "Ares data type",
        Documentation = $"Ares data type '{typeName}'.",
        Kind = SymbolKind.Type,
        SortText = $"00_{typeName}"
      });
    }
  }

  private static bool IsTypeHintContext(string script, int cursorLine, int cursorColumn)
  {
    if(cursorLine <= 0 || cursorColumn <= 0 || string.IsNullOrEmpty(script))
    {
      return false;
    }

    var lines = script.Split(["\r\n", "\n"], StringSplitOptions.None);
    if(cursorLine > lines.Length)
    {
      return false;
    }

    var line = lines[cursorLine - 1];
    var safeColumn = Math.Min(cursorColumn - 1, line.Length);
    var prefix = line[..safeColumn];
    if(!prefix.Contains("def", StringComparison.Ordinal))
    {
      return false;
    }

    var defIndex = prefix.IndexOf("def", StringComparison.Ordinal);
    var openParenIndex = prefix.IndexOf('(', defIndex);
    if(defIndex < 0 || openParenIndex < 0)
    {
      return false;
    }

    var closeParenIndex = prefix.LastIndexOf(')');
    if(closeParenIndex < openParenIndex)
    {
      var parameterHintColonIndex = prefix.LastIndexOf(':');
      return parameterHintColonIndex > openParenIndex;
    }

    var returnHintArrowIndex = prefix.IndexOf("->", closeParenIndex + 1, StringComparison.Ordinal);
    if(returnHintArrowIndex < 0)
    {
      return false;
    }

    var suffixAfterReturnHintArrow = prefix[(returnHintArrowIndex + 2)..];
    return !suffixAfterReturnHintArrow.Contains(':');
  }

  [GeneratedRegex(@"([A-Za-z_][A-Za-z0-9_]*)\s*$")]
  private static partial Regex IdentifierRegex();
}
