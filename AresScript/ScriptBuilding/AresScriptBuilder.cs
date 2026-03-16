using Antlr4.Runtime;
using Ares.Datamodel;
using AresScript.Generated;
using AresScript.Symbols;

namespace AresScript.ScriptBuilding;

public sealed class AresScriptBuilder : AresScriptBlockBuilder
{
  private const string _functionPrefix = "def ";

  public AresScriptBuilder(int indentSize = 2)
    : base([], indentSize, ScriptBuilderCapabilities.Root)
  {
  }

  private AresScriptBuilder(List<ScriptNode> statements, int indentSize)
    : base(statements, indentSize, ScriptBuilderCapabilities.Root)
  {
  }

  public static AresScriptBuilder FromScript(string script, int indentSize = 2)
  {
    if(string.IsNullOrWhiteSpace(script))
    {
      return new AresScriptBuilder(indentSize);
    }

    var input = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(input);
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var program = parser.program();
    if(parser.NumberOfSyntaxErrors > 0)
    {
      throw new ArgumentException("Script contains syntax errors and cannot be imported.", nameof(script));
    }

    var statements = ScriptImportConverter.Convert(program);
    return new AresScriptBuilder(statements, indentSize);
  }

  public bool RemoveFunction(string name)
  {
    if(string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Function name cannot be null, empty, or whitespace.", nameof(name));
    }

    var index = FindFunctionIndex(name.Trim(), out _, out _, out _);
    if(index < 0)
    {
      return false;
    }

    MutableStatements.RemoveAt(index);
    return true;
  }

  public bool EditFunction(string name, Action<AresScriptBlockBuilder> configureBody)
  {
    ArgumentNullException.ThrowIfNull(configureBody);
    if(string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Function name cannot be null, empty, or whitespace.", nameof(name));
    }

    var index = FindFunctionIndex(name.Trim(), out var existingParameters, out _, out var existingReturnTypeHint);
    if(index < 0)
    {
      return false;
    }

    var functionBody = MutableStatements[index] is BlockNode block
      ? block.Statements.ToList()
      : [];
    var bodyBuilder = new AresScriptBlockBuilder(functionBody, IndentSize, new ScriptBuilderCapabilities(AllowReturn: true, AllowLoopControl: false));
    configureBody(bodyBuilder);

    if(functionBody.Count == 0)
    {
      throw new InvalidOperationException("Function body must contain at least one statement.");
    }

    var returnSchema = string.IsNullOrWhiteSpace(existingReturnTypeHint)
      ? null
      : AresScriptTypeHints.SchemaFromTypeHint(existingReturnTypeHint);
    var signature = ScriptBuildingHelpers.BuildFunctionSignature(name, existingParameters, returnSchema);
    MutableStatements[index] = new BlockNode(signature, functionBody);
    return true;
  }

  public bool ReplaceFunction(string name, Action<AresScriptBlockBuilder> configureBody, params AresScriptParameter[] parameters)
  {
    return ReplaceFunction(name, configureBody, null, parameters);
  }

  public bool ReplaceFunction(
    string name,
    Action<AresScriptBlockBuilder> configureBody,
    SchemaEntry? returnSchema,
    params AresScriptParameter[] parameters)
  {
    ArgumentNullException.ThrowIfNull(configureBody);
    ArgumentNullException.ThrowIfNull(parameters);
    if(string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Function name cannot be null, empty, or whitespace.", nameof(name));
    }

    var safeName = name.Trim();
    var index = FindFunctionIndex(safeName, out _, out _, out _);
    if(index < 0)
    {
      return false;
    }

    MutableStatements[index] = BuildFunctionNode(safeName, configureBody, returnSchema, parameters);
    return true;
  }

  public bool AddOrReplaceFunction(string name, Action<AresScriptBlockBuilder> configureBody, params AresScriptParameter[] parameters)
  {
    return AddOrReplaceFunction(name, configureBody, null, parameters);
  }

  public bool AddOrReplaceFunction(
    string name,
    Action<AresScriptBlockBuilder> configureBody,
    SchemaEntry? returnSchema,
    params AresScriptParameter[] parameters)
  {
    ArgumentNullException.ThrowIfNull(configureBody);
    ArgumentNullException.ThrowIfNull(parameters);
    if(string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Function name cannot be null, empty, or whitespace.", nameof(name));
    }

    var safeName = name.Trim();
    var functionNode = BuildFunctionNode(safeName, configureBody, returnSchema, parameters);

    var index = FindFunctionIndex(safeName, out _, out _, out _);
    if(index >= 0)
    {
      MutableStatements[index] = functionNode;
    }
    else
    {
      MutableStatements.Add(functionNode);
    }

    return true;
  }

  private BlockNode BuildFunctionNode(
    string functionName,
    Action<AresScriptBlockBuilder> configureBody,
    SchemaEntry? returnSchema,
    IReadOnlyCollection<AresScriptParameter> parameters)
  {
    var safeParameters = parameters.ToArray();
    var bodyNodes = new List<ScriptNode>();
    var bodyBuilder = new AresScriptBlockBuilder(bodyNodes, IndentSize, new ScriptBuilderCapabilities(AllowReturn: true, AllowLoopControl: false));
    configureBody(bodyBuilder);
    if(bodyNodes.Count == 0)
    {
      throw new InvalidOperationException("Function body must contain at least one statement.");
    }

    var signature = ScriptBuildingHelpers.BuildFunctionSignature(functionName, safeParameters, returnSchema);
    return new BlockNode(signature, bodyNodes);
  }

  private int FindFunctionIndex(string functionName, out AresScriptParameter[] parameters, out string normalizedName, out string returnTypeHint)
  {
    for(var i = 0; i < MutableStatements.Count; i++)
    {
      if(MutableStatements[i] is not BlockNode block)
      {
        continue;
      }

      if(!TryParseFunctionHeader(block.Header, out normalizedName, out parameters, out returnTypeHint))
      {
        continue;
      }

      if(string.Equals(normalizedName, functionName, StringComparison.Ordinal))
      {
        return i;
      }
    }

    parameters = [];
    normalizedName = string.Empty;
    returnTypeHint = string.Empty;
    return -1;
  }

  private static bool TryParseFunctionHeader(string header, out string functionName, out AresScriptParameter[] parameters, out string returnTypeHint)
  {
    functionName = string.Empty;
    parameters = [];
    returnTypeHint = string.Empty;

    if(!header.StartsWith(_functionPrefix, StringComparison.Ordinal))
    {
      return false;
    }

    var input = new AntlrInputStream($"{header}:\n  return None\n");
    var lexer = new AresIndentationLexer(input);
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var program = parser.program();
    if(parser.NumberOfSyntaxErrors > 0
      || program.statement().FirstOrDefault() is not AresLangParser.SimpleStmtContext simpleStatement
      || simpleStatement.simpleStatement() is not AresLangParser.FunctionDeclContext functionDecl)
    {
      return false;
    }

    var declaration = functionDecl.functionDeclaration();
    functionName = declaration.ID().GetText();
    parameters = (declaration.parameterList()?.parameter() ?? [])
      .Select(ScriptBuildingHelpers.ToScriptParameter)
      .ToArray();
    returnTypeHint = declaration.typeHint()?.GetText() ?? string.Empty;
    return true;
  }
}
