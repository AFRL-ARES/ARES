using Antlr4.Runtime;
using AresScript.Generated;

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

    var index = FindFunctionIndex(name.Trim(), out _, out _);
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

    var index = FindFunctionIndex(name.Trim(), out var existingParameters, out _);
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

    var signature = existingParameters.Length == 0
      ? $"{_functionPrefix}{name}()"
      : $"{_functionPrefix}{name}({string.Join(", ", existingParameters)})";
    MutableStatements[index] = new BlockNode(signature, functionBody);
    return true;
  }

  public bool ReplaceFunction(string name, Action<AresScriptBlockBuilder> configureBody, params string[] parameters)
  {
    ArgumentNullException.ThrowIfNull(configureBody);
    ArgumentNullException.ThrowIfNull(parameters);
    if(string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Function name cannot be null, empty, or whitespace.", nameof(name));
    }

    var safeName = name.Trim();
    var safeParameters = parameters.Select(p => p.Trim()).ToArray();
    var bodyNodes = new List<ScriptNode>();
    var bodyBuilder = new AresScriptBlockBuilder(bodyNodes, IndentSize, new ScriptBuilderCapabilities(AllowReturn: true, AllowLoopControl: false));
    configureBody(bodyBuilder);
    if(bodyNodes.Count == 0)
    {
      throw new InvalidOperationException("Function body must contain at least one statement.");
    }

    var signature = safeParameters.Length == 0
      ? $"{_functionPrefix}{safeName}()"
      : $"{_functionPrefix}{safeName}({string.Join(", ", safeParameters)})";
    var functionNode = new BlockNode(signature, bodyNodes);

    var index = FindFunctionIndex(safeName, out _, out _);
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

  private int FindFunctionIndex(string functionName, out string[] parameters, out string normalizedName)
  {
    for(var i = 0; i < MutableStatements.Count; i++)
    {
      if(MutableStatements[i] is not BlockNode block)
      {
        continue;
      }

      if(!TryParseFunctionHeader(block.Header, out normalizedName, out parameters))
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
    return -1;
  }

  private static bool TryParseFunctionHeader(string header, out string functionName, out string[] parameters)
  {
    functionName = string.Empty;
    parameters = [];

    if(!header.StartsWith(_functionPrefix, StringComparison.Ordinal))
    {
      return false;
    }

    var leftParenIndex = header.IndexOf('(');
    var rightParenIndex = header.LastIndexOf(')');
    if(leftParenIndex < _functionPrefix.Length || rightParenIndex <= leftParenIndex)
    {
      return false;
    }

    functionName = header[_functionPrefix.Length..leftParenIndex].Trim();
    var paramsText = header[(leftParenIndex + 1)..rightParenIndex].Trim();
    if(string.IsNullOrEmpty(paramsText))
    {
      return true;
    }

    parameters = paramsText
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .ToArray();
    return true;
  }
}
