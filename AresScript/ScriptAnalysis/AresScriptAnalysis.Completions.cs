using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using System.Text;
using System.Text.RegularExpressions;
using AresScript;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static async Task<AresScriptEnvironment> BuildEnvironmentForCompletions(AresScriptEnvironment environment, string script)
  {
    if(string.IsNullOrWhiteSpace(script))
    {
      return environment;
    }

    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      var programCtx = parser.program();
      var validator = new AresValidationInterpreter(environment, AresValidationInterpreter.ValidationMode.Lenient);
      await validator.Visit(programCtx);
    }
    catch
    {
      // Ignore parse/validation errors for autocomplete; fall back to system scope.
    }

    return environment;
  }

  public static async Task<IReadOnlyList<CompletionItem>> BuildCompletionsAsync(
    AresScriptEnvironment environment,
    string script,
    int cursorLine,
    int cursorColumn)
  {
    await BuildEnvironmentForCompletions(environment, script);
    var systemVariables = environment.GetAllSystemVariables();
    var userFunctions = environment.GetAllUserFunctions();
    var userVariables = environment.GetAllUserVariableNames();
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
      }
      else if(environment.TryGetValue(parentIdentifier, out var parentValue)
        && parentValue.KindCase == AresValue.KindOneofCase.StructValue
        && parentValue.StructValue is not null)
      {
        foreach(var field in parentValue.StructValue.Fields)
        {
          AddCompletionForAresValue(items, environment, field.Key, field.Value, parentIdentifier);
        }
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
        Kind = CompletionItemKind.Function
      }));

      items.AddRange(userVariables.Select(name => new CompletionItem
      {
        Label = name,
        InsertText = name,
        Detail = "User variable",
        Kind = CompletionItemKind.Variable
      }));
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
        Kind = CompletionItemKind.Function,
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
        ? CompletionItemKind.Struct
        : CompletionItemKind.Variable,
      ParentIdentifier = parentIdentifier,
      Schema = ValueToSchemaEntry(schemaValue)
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
        Kind = CompletionItemKind.Function,
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
        ? CompletionItemKind.Struct
        : CompletionItemKind.Variable,
      ParentIdentifier = parentIdentifier,
      Schema = ValueToSchemaEntry(value)
    });
  }

  [GeneratedRegex(@"([A-Za-z_][A-Za-z0-9_]*)\s*$")]
  private static partial Regex IdentifierRegex();
}
