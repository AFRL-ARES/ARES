using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using AresScript.Environment;
using AresScript.Generated;
using AresScript.Interpreters;
using AresScript.Symbols;

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
      return BuildFunctionMetadataForSymbolMetadata(
        identifier,
        string.Empty,
        systemFunction.Detail ?? string.Empty,
        systemFunction.Documentation ?? string.Empty,
        systemFunction.InputSchema,
        systemFunction.OutputSchema);
    }

    if(environment.TryGetUserFunction(identifier, out var userFunction))
    {
      return BuildFunctionMetadataForSymbolMetadata(
        identifier,
        string.Empty,
        "User function",
        "User-defined function.",
        BuildUserFunctionInputSchema(userFunction),
        AresSchemaBuilder.Entry(userFunction.ReturnType).Build(),
        isUserDefined: true);
    }

    if(environment.TryGetUserLambda(identifier, out var lambda))
    {
      var lambdaSchema = new AresDataSchema();
      foreach(var parameter in lambda.Parameters)
      {
        lambdaSchema.Fields[parameter] = AresSchemaBuilder.Entry(AresDataType.Any).Build();
      }

      return BuildFunctionMetadataForSymbolMetadata(
        identifier,
        string.Empty,
        "Lambda function",
        "User-defined lambda.",
        lambdaSchema,
        AresSchemaBuilder.Entry(AresDataType.Any).Build(),
        isUserDefined: true,
        isLambda: true);
    }

    if(environment.TryGetValue(identifier, out var value))
    {
      return BuildValueSymbolMetadata(identifier, string.Empty, value);
    }

    var inferredSchema = TryInferSchema(script, cursorLine, cursorColumn, identifier, environment);
    if(inferredSchema is not null)
    {
      return BuildValueMetadataForSymbolMetadata(
        identifier,
        parentIdentifier ?? string.Empty,
        string.Empty,
        string.Empty,
        inferredSchema.Type == AresDataType.Struct ? SymbolKind.Struct : SymbolKind.Variable,
        inferredSchema);
    }

    return new ScriptSymbolMetadata
    {
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
    foreach(var (name, value) in environment.GetAllUserVariableSymbols())
    {
      schemas[name] = value.DeclaredSchema ?? value.Value.ToSchemaEntry();
    }

    foreach(var (name, systemValue) in environment.GetAllSystemVariables())
    {
      schemas[name] = systemValue.DeclaredSchema ?? systemValue.Value.ToSchemaEntry();
    }

    return schemas;
  }

  private static ScriptSymbolMetadata? TryResolveMemberSymbol(
    AresScriptEnvironment environment,
    string parentIdentifier,
    string memberIdentifier)
  {
    if(TryResolveSystemParentValue(environment, parentIdentifier, out var systemParent)
      && systemParent.ValueKind == AresSystemValue.AresSystemValueKind.Struct
      && systemParent.StructFields is not null
      && systemParent.StructFields.TryGetValue(memberIdentifier, out var systemMember))
    {
      if(systemMember.RawValue?.FunctionValue is not null
        && environment.TryGetSystemFunction(systemMember.RawValue.FunctionValue.FunctionId, out var systemFunction))
      {
        return BuildFunctionMetadataForSymbolMetadata(
          memberIdentifier,
          parentIdentifier,
          systemFunction.Detail ?? string.Empty,
          systemFunction.Documentation ?? string.Empty,
          systemFunction.InputSchema,
          systemFunction.OutputSchema);
      }

      var normalizedSystemMember = systemMember with
      {
        Name = string.IsNullOrWhiteSpace(systemMember.Name) ? memberIdentifier : systemMember.Name,
        ParentName = parentIdentifier
      };

      return ScriptSymbolMetadataMapper.ToMetadata(normalizedSystemMember, parentIdentifier: parentIdentifier);
    }

    if(!TryResolveValue(environment, parentIdentifier, out var parentValue))
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
      return BuildFunctionMetadataForSymbolMetadata(
        memberIdentifier,
        parentIdentifier,
        extensionFunction.Detail ?? string.Empty,
        extensionFunction.Documentation ?? string.Empty,
        TrimReceiverFromSchema(extensionFunction.InputSchema),
        extensionFunction.OutputSchema,
        isExtension: true);
    }

    return null;
  }

  private static ScriptSymbolMetadata BuildValueSymbolMetadata(string identifier, string parentIdentifier, AresValue value)
  {
    var schema = value.ToSchemaEntry();
    return BuildValueMetadataForSymbolMetadata(
      identifier,
      parentIdentifier,
      string.Empty,
      string.Empty,
      schema.Type == AresDataType.Struct ? SymbolKind.Struct : SymbolKind.Variable,
      schema,
      value);
  }

  private static ScriptSymbolMetadata BuildFunctionMetadataForSymbolMetadata(
    string identifier,
    string parentIdentifier,
    string detail,
    string documentation,
    AresDataSchema inputSchema,
    SchemaEntry outputSchema,
    bool isExtension = false,
    bool isUserDefined = false,
    bool isLambda = false)
  {
    var metadata = new ScriptSymbolMetadata
    {
      Identifier = identifier,
      ParentIdentifier = parentIdentifier,
      Kind = SymbolKind.Function,
      Detail = detail,
      Documentation = documentation,
      FunctionShape = new ScriptSymbolMetadata.Types.FunctionShape
      {
        InputSchema = inputSchema,
        OutputSchema = outputSchema
      }
    };

    if(isExtension)
    {
      metadata.Tags.Add(SymbolTag.Extension);
    }

    if(isUserDefined)
    {
      metadata.Tags.Add(SymbolTag.UserDefined);
    }

    if(isLambda)
    {
      metadata.Tags.Add(SymbolTag.Lambda);
    }

    return metadata;
  }

  private static ScriptSymbolMetadata BuildValueMetadataForSymbolMetadata(
    string identifier,
    string parentIdentifier,
    string detail,
    string documentation,
    SymbolKind kind,
    SchemaEntry schema,
    AresValue? value = null)
  {
    var metadata = new ScriptSymbolMetadata
    {
      Identifier = identifier,
      ParentIdentifier = parentIdentifier,
      Kind = kind,
      Detail = detail,
      Documentation = documentation,
      ValueShape = new ScriptSymbolMetadata.Types.ValueShape
      {
        Schema = schema
      }
    };

    if(value is not null)
    {
      metadata.ValueShape.Value = value;
    }

    return metadata;
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
