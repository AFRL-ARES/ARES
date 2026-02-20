using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using AresScript.Generated;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static async Task<ScriptSymbolMetadata> BuildSymbolMetadataAsync(
    AresScriptEnvironment environment,
    string script,
    int cursorLine,
    int cursorColumn,
    string? identifier = null)
  {
    identifier = string.IsNullOrWhiteSpace(identifier)
      ? TryGetIdentifierAtCursor(script, cursorLine, cursorColumn)
      : identifier;
    if(string.IsNullOrWhiteSpace(identifier))
    {
      return new ScriptSymbolMetadata
      {
        Found = false,
        Identifier = string.Empty,
        ParentIdentifier = string.Empty,
        Kind = SymbolKind.Unspecified,
        Detail = string.Empty,
        Documentation = string.Empty
      };
    }

    await BuildEnvironmentForCompletions(environment, script, cursorLine);
    TryGetParentIdentifier(script, cursorLine, cursorColumn, out var parentIdentifier);

    if(!string.IsNullOrWhiteSpace(parentIdentifier))
    {
      var memberMetadata = TryResolveMemberSymbol(environment, parentIdentifier, identifier);
      if(memberMetadata is not null)
      {
        return memberMetadata;
      }
    }

    if(environment.TryGetSystemFunction(identifier, out var systemFunction))
    {
      return new ScriptSymbolMetadata
      {
        Found = true,
        Identifier = identifier,
        ParentIdentifier = string.Empty,
        Kind = SymbolKind.Function,
        Detail = systemFunction.Description ?? string.Empty,
        Documentation = systemFunction.Description ?? string.Empty,
        InputSchema = systemFunction.InputSchema,
        OutputSchema = systemFunction.OutputSchema
      };
    }

    if(environment.TryGetUserFunction(identifier, out var userFunction))
    {
      return new ScriptSymbolMetadata
      {
        Found = true,
        Identifier = identifier,
        ParentIdentifier = string.Empty,
        Kind = SymbolKind.Function,
        Detail = "User function",
        Documentation = "User-defined function.",
        InputSchema = BuildUserFunctionInputSchema(userFunction),
        OutputSchema = AresSchemaBuilder.Entry(userFunction.ReturnType).Build()
      };
    }

    if(environment.TryGetUserLambda(identifier, out var lambda))
    {
      var lambdaSchema = new AresDataSchema();
      foreach(var parameter in lambda.Parameters)
      {
        lambdaSchema.Fields[parameter] = AresSchemaBuilder.Entry(AresDataType.Any).Build();
      }

      return new ScriptSymbolMetadata
      {
        Found = true,
        Identifier = identifier,
        ParentIdentifier = string.Empty,
        Kind = SymbolKind.Function,
        Detail = "Lambda function",
        Documentation = "User-defined lambda.",
        InputSchema = lambdaSchema,
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Any).Build()
      };
    }

    if(environment.TryGetValue(identifier, out var value))
    {
      return BuildValueSymbolMetadata(identifier, string.Empty, value);
    }

    var inferredSchema = TryInferSchema(script, cursorLine, cursorColumn, identifier, environment);
    if(inferredSchema is not null)
    {
      return new ScriptSymbolMetadata
      {
        Found = true,
        Identifier = identifier,
        ParentIdentifier = parentIdentifier ?? string.Empty,
        Kind = inferredSchema.Type == AresDataType.Struct ? SymbolKind.Struct : SymbolKind.Variable,
        Detail = string.Empty,
        Documentation = string.Empty,
        Schema = inferredSchema
      };
    }

    return new ScriptSymbolMetadata
    {
      Found = false,
      Identifier = identifier,
      ParentIdentifier = parentIdentifier ?? string.Empty,
      Kind = SymbolKind.Unspecified,
      Detail = string.Empty,
      Documentation = string.Empty
    };
  }

  private static string? TryGetIdentifierAtCursor(string script, int cursorLine, int cursorColumn)
  {
    if(cursorLine <= 0 || cursorColumn <= 0 || string.IsNullOrEmpty(script))
    {
      return null;
    }

    var lines = script.Split(["\r\n", "\n"], StringSplitOptions.None);
    if(cursorLine > lines.Length)
    {
      return null;
    }

    var line = lines[cursorLine - 1];
    if(string.IsNullOrEmpty(line))
    {
      return null;
    }

    var index = Math.Clamp(cursorColumn - 1, 0, line.Length);
    if(index == line.Length && index > 0)
    {
      index--;
    }

    if(!IsIdentifierChar(line[index]) && index > 0 && IsIdentifierChar(line[index - 1]))
    {
      index--;
    }

    if(!IsIdentifierChar(line[index]))
    {
      return null;
    }

    var start = index;
    while(start > 0 && IsIdentifierChar(line[start - 1]))
    {
      start--;
    }

    var end = index;
    while(end + 1 < line.Length && IsIdentifierChar(line[end + 1]))
    {
      end++;
    }

    return line[start..(end + 1)];
  }

  private static bool IsIdentifierChar(char c)
  {
    return char.IsLetterOrDigit(c) || c == '_';
  }

  private static SchemaEntry? TryInferSchema(
    string script,
    int line,
    int column,
    string identifier,
    AresScriptEnvironment environment)
  {
    var programCtx = TryParseProgram(script);
    if(programCtx is null)
    {
      return null;
    }

    var collector = new VariableSchemaCollector(environment, BuildGlobalSchemas(environment), line, column, identifier);
    collector.Visit(programCtx);
    return collector.FoundSchema;
  }

  private static AresLangParser.ProgramContext? TryParseProgram(string script)
  {
    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      return parser.program();
    }
    catch
    {
      return null;
    }
  }

  private static Dictionary<string, SchemaEntry> BuildGlobalSchemas(AresScriptEnvironment environment)
  {
    var schemas = new Dictionary<string, SchemaEntry>(StringComparer.Ordinal);
    foreach(var (name, value) in environment.GetAllUserVariables())
    {
      schemas[name] = value.ToSchemaEntry();
    }

    foreach(var (name, systemValue) in environment.GetAllSystemVariables())
    {
      schemas[name] = systemValue.ToAresValue().ToSchemaEntry();
    }

    return schemas;
  }

  private static ScriptSymbolMetadata? TryResolveMemberSymbol(
    AresScriptEnvironment environment,
    string parentIdentifier,
    string memberIdentifier)
  {
    if(!environment.TryGetValue(parentIdentifier, out var parentValue))
    {
      return null;
    }

    if(parentValue.StructValue is not null
      && parentValue.StructValue.Fields.TryGetValue(memberIdentifier, out var structMember))
    {
      return BuildValueSymbolMetadata(memberIdentifier, parentIdentifier, structMember);
    }

    if(environment.TryGetExtensionFunction(parentValue, memberIdentifier, out var extensionFunction))
    {
      return new ScriptSymbolMetadata
      {
        Found = true,
        Identifier = memberIdentifier,
        ParentIdentifier = parentIdentifier,
        Kind = SymbolKind.Function,
        Detail = extensionFunction.Description ?? string.Empty,
        Documentation = extensionFunction.Description ?? string.Empty,
        InputSchema = TrimReceiverFromSchema(extensionFunction.InputSchema),
        OutputSchema = extensionFunction.OutputSchema
      };
    }

    return null;
  }

  private static ScriptSymbolMetadata BuildValueSymbolMetadata(string identifier, string parentIdentifier, AresValue value)
  {
    var schema = value.ToSchemaEntry();
    return new ScriptSymbolMetadata
    {
      Found = true,
      Identifier = identifier,
      ParentIdentifier = parentIdentifier,
      Kind = schema.Type == AresDataType.Struct ? SymbolKind.Struct : SymbolKind.Variable,
      Detail = string.Empty,
      Documentation = string.Empty,
      Schema = schema
    };
  }

  private static AresDataSchema BuildUserFunctionInputSchema(AresScriptFunction userFunction)
  {
    var schema = new AresDataSchema();
    foreach(var parameter in userFunction.Parameters)
    {
      schema.Fields[parameter.Name] = AresSchemaBuilder.Entry(parameter.Type).Build();
    }

    return schema;
  }
}
