using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using System.Text.RegularExpressions;

namespace AresScript;

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

  public static AutocompleteCatalog BuildAutocompleteCatalog(AresScriptEnvironment env)
  {
    var systemFunctions = env.GetAllSystemFunctions();
    var systemVariables = env.GetAllSystemVariables();
    var namespaceMap = new Dictionary<string, NamespaceSymbol>(StringComparer.Ordinal);

    foreach(var func in systemFunctions)
    {
      var namespaceName = func.Namespace;
      if(!namespaceMap.TryGetValue(func.Namespace, out var ns))
      {
        ns = new NamespaceSymbol
        {
          NamespaceId = namespaceName,
          Identifier = namespaceName,
          DisplayName = namespaceName,
          Description = string.Empty,
          Kind = NamespaceKind.Device
        };
        namespaceMap[namespaceName] = ns;
      }

      ns.Functions.Add(new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      });
    }

    var response = new AutocompleteCatalog
    {
      CatalogVersion = string.Empty
    };
    response.Namespaces.AddRange(namespaceMap.Values);
    response.GlobalFunctions.AddRange(systemFunctions
      .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
      .Select(func => new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      }));
    response.Globals.AddRange(systemVariables.Select(kv => new GlobalVariableSymbol
    {
      Name = kv.Key,
      Description = string.Empty,
      Schema = ValueToSchemaEntry(kv.Value),
      Value = kv.Value
    }));
    return response;
  }

  public static CompletionItemKind MapNamespaceKindToCompletionKind(NamespaceKind kind)
  {
    return kind switch
    {
      NamespaceKind.Device => CompletionItemKind.Device,
      NamespaceKind.Planner => CompletionItemKind.Planner,
      NamespaceKind.Analyzer => CompletionItemKind.Analyzer,
      _ => CompletionItemKind.Unspecified
    };
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

  public static async Task<IReadOnlyList<Diagnostic>> ValidateScriptAsync(
    string? script,
    AresScriptEnvironment environment,
    AresValidationInterpreter.ValidationMode mode = AresValidationInterpreter.ValidationMode.Strict)
  {
    var diagnostics = new List<Diagnostic>();

    var stream = new AntlrInputStream(script ?? string.Empty);
    var lexer = new AresIndentationLexer(stream);
    var lexerListener = new CollectingLexerErrorListener(diagnostics);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(lexerListener);

    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var parserListener = new CollectingParserErrorListener(diagnostics);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(parserListener);

    AresLangParser.ProgramContext? programCtx = null;
    try
    {
      programCtx = parser.program();
    }
    catch(Exception ex)
    {
      AppendDiagnosticFromException(diagnostics, ex);
    }

    if(programCtx is not null)
    {
      var validator = new AresValidationInterpreter(environment, mode);
      try
      {
        await validator.Visit(programCtx);
      }
      catch(Exception ex)
      {
        AppendDiagnosticFromException(diagnostics, ex);
      }
    }

    return diagnostics;
  }

  private static string ExtractTrailingIdentifier(string text)
  {
    var match = IdentifierRegex().Match(text);
    return match.Success ? match.Groups[1].Value : string.Empty;
  }

  private static SchemaEntry ValueToSchemaEntry(AresValue value)
  {
    return value.KindCase switch
    {
      AresValue.KindOneofCase.NullValue => AresSchemaBuilder.Entry(AresDataType.Null).Build(),
      AresValue.KindOneofCase.BoolValue => AresSchemaBuilder.Entry(AresDataType.Boolean).Build(),
      AresValue.KindOneofCase.StringValue => AresSchemaBuilder.Entry(AresDataType.String).Build(),
      AresValue.KindOneofCase.NumberValue => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      AresValue.KindOneofCase.StringArrayValue => AresSchemaBuilder.Entry(AresDataType.StringArray).Build(),
      AresValue.KindOneofCase.NumberArrayValue => AresSchemaBuilder.Entry(AresDataType.NumberArray).Build(),
      AresValue.KindOneofCase.BytesValue => AresSchemaBuilder.Entry(AresDataType.ByteArray).Build(),
      AresValue.KindOneofCase.UnitValue => AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      AresValue.KindOneofCase.ListValue => CreateListEntry(value.ListValue.Values),
      AresValue.KindOneofCase.StructValue => CreateStructEntry(value.StructValue),
      _ => AresSchemaBuilder.Entry(AresDataType.Any).Build()
    };
  }

  private static SchemaEntry CreateStructEntry(AresStruct structValue)
  {
    var schema = new AresDataSchema();
    foreach(var field in structValue.Fields)
    {
      schema.Fields[field.Key] = ValueToSchemaEntry(field.Value);
    }

    var entry = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
    entry.StructSchema = schema;
    return entry;
  }

  private static SchemaEntry CreateListEntry(IEnumerable<AresValue> values)
  {
    var list = values.ToArray();
    if(list.Length == 0)
    {
      return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
    }

    var first = ValueToSchemaEntry(list[0]);
    var allSameType = list.All(val => ValueToSchemaEntry(val).Type == first.Type);
    if(allSameType)
    {
      return CreateListEntry(first);
    }

    return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
  }

  private static SchemaEntry CreateListEntry(SchemaEntry elementSchema)
  {
    var entry = AresSchemaBuilder.Entry(AresDataType.List).Build();
    entry.ListElementSchema = elementSchema;
    return entry;
  }

  [GeneratedRegex(@"([A-Za-z_][A-Za-z0-9_]*)\s*$")]
  private static partial Regex IdentifierRegex();

  [GeneratedRegex(@"(\d+):(\d+)\s*$")]
  private static partial Regex LineColumnRegex();

  private static void AppendDiagnosticFromException(ICollection<Diagnostic> diagnostics, Exception ex)
  {
    var message = ex.Message ?? "Validation error";
    var line = 1;
    var column = 1;

    var match = LineColumnRegex().Match(message);
    if(match.Success
      && int.TryParse(match.Groups[1].Value, out var parsedLine)
      && int.TryParse(match.Groups[2].Value, out var parsedColumn))
    {
      line = parsedLine;
      column = parsedColumn;
    }

    diagnostics.Add(new Diagnostic
    {
      StartLine = line,
      StartColumn = column,
      EndLine = line,
      EndColumn = column,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    });
  }

  private sealed class CollectingLexerErrorListener : IAntlrErrorListener<int>
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingLexerErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg));
    }
  }

  private sealed class CollectingParserErrorListener : BaseErrorListener
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingParserErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg, offendingSymbol));
    }
  }

  private static Diagnostic CreateDiagnostic(int line, int charPositionInLine, string message, IToken? offendingSymbol = null)
  {
    var startColumn = Math.Max(1, charPositionInLine + 1);
    var tokenLength = offendingSymbol?.Text?.Length ?? 1;
    var endColumn = Math.Max(startColumn, startColumn + tokenLength - 1);

    return new Diagnostic
    {
      StartLine = Math.Max(1, line),
      StartColumn = startColumn,
      EndLine = Math.Max(1, line),
      EndColumn = endColumn,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    };
  }
}
