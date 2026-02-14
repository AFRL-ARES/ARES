using System.Text.RegularExpressions;
using System.Text;

namespace AresScript.ScriptBuilding;

public class AresScriptBlockBuilder
{
  private static readonly Regex _identifierRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

  private readonly List<ScriptNode> _statements;
  private readonly int _indentSize;
  private readonly ScriptBuilderCapabilities _capabilities;

  internal AresScriptBlockBuilder(List<ScriptNode> statements, int indentSize, ScriptBuilderCapabilities capabilities)
  {
    ArgumentNullException.ThrowIfNull(statements);
    if(indentSize < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(indentSize), indentSize, "Indent size must be at least 1.");
    }

    _statements = statements;
    _indentSize = indentSize;
    _capabilities = capabilities;
  }

  internal IReadOnlyList<ScriptNode> Statements => _statements;

  public string Build()
  {
    if(_statements.Count == 0)
    {
      return string.Empty;
    }

    var output = new StringBuilder();
    foreach(var statement in _statements)
    {
      statement.WriteTo(output, indentLevel: 0, _indentSize);
    }

    return output.ToString().TrimEnd('\r', '\n');
  }

  public AresScriptBlockBuilder AddComment(string comment)
  {
    var safeComment = EnsureText(comment, nameof(comment));
    _statements.Add(new LineNode($"# {safeComment}"));
    return this;
  }

  public AresScriptBlockBuilder AddAssignment(string lvalue, string expression)
  {
    var safeLvalue = EnsureText(lvalue, nameof(lvalue));
    var safeExpression = EnsureText(expression, nameof(expression));
    _statements.Add(new LineNode($"{safeLvalue} = {safeExpression}"));
    return this;
  }

  public AresScriptBlockBuilder AddExpression(string expression)
  {
    var safeExpression = EnsureText(expression, nameof(expression));
    _statements.Add(new LineNode(safeExpression));
    return this;
  }

  public AresScriptBlockBuilder AddAssert(string conditionExpression, string? failureMessageExpression = null)
  {
    var safeCondition = EnsureText(conditionExpression, nameof(conditionExpression));
    var safeFailureMessage = failureMessageExpression is null ? null : EnsureText(failureMessageExpression, nameof(failureMessageExpression));
    _statements.Add(safeFailureMessage is null
      ? new LineNode($"assert {safeCondition}")
      : new LineNode($"assert {safeCondition}, {safeFailureMessage}"));
    return this;
  }

  public AresScriptBlockBuilder AddReturn(string? expression = null)
  {
    if(!_capabilities.AllowReturn)
    {
      throw new InvalidOperationException("Return statements can only be added inside function bodies.");
    }

    var returnStatement = expression is null ? "return" : $"return {EnsureText(expression, nameof(expression))}";
    _statements.Add(new LineNode(returnStatement));
    return this;
  }

  public AresScriptBlockBuilder AddBreak()
  {
    if(!_capabilities.AllowLoopControl)
    {
      throw new InvalidOperationException("Break statements can only be added inside loop bodies.");
    }

    _statements.Add(new LineNode("break"));
    return this;
  }

  public AresScriptBlockBuilder AddContinue()
  {
    if(!_capabilities.AllowLoopControl)
    {
      throw new InvalidOperationException("Continue statements can only be added inside loop bodies.");
    }

    _statements.Add(new LineNode("continue"));
    return this;
  }

  public AresScriptBlockBuilder AddFunction(string name, Action<AresScriptBlockBuilder> configureBody, params string[] parameters)
  {
    return AddFunction(name, (IEnumerable<string>)parameters, configureBody);
  }

  public AresScriptBlockBuilder AddFunction(string name, IEnumerable<string> parameters, Action<AresScriptBlockBuilder> configureBody)
  {
    var safeName = ValidateIdentifier(name, nameof(name));
    ArgumentNullException.ThrowIfNull(parameters);
    ArgumentNullException.ThrowIfNull(configureBody);

    var normalizedParameters = parameters
      .Select((parameter, index) => ValidateIdentifier(parameter, $"{nameof(parameters)}[{index}]"))
      .ToArray();

    var body = CreateChildBlockBuilder(allowReturn: true, allowLoopControl: false);
    configureBody(body);
    EnsureHasStatements(body, "Function body must contain at least one statement.");

    var signature = normalizedParameters.Length == 0
      ? $"def {safeName}()"
      : $"def {safeName}({string.Join(", ", normalizedParameters)})";
    _statements.Add(new BlockNode(signature, body.Statements));
    return this;
  }

  public AresScriptBlockBuilder AddIf(
    string conditionExpression,
    Action<AresScriptBlockBuilder> configureThenBranch,
    Action<AresConditionalBuilder>? configureBranches = null)
  {
    var safeCondition = EnsureText(conditionExpression, nameof(conditionExpression));
    ArgumentNullException.ThrowIfNull(configureThenBranch);

    var thenBranch = CreateChildBlockBuilder(_capabilities.AllowReturn, _capabilities.AllowLoopControl);
    configureThenBranch(thenBranch);
    EnsureHasStatements(thenBranch, "If branch must contain at least one statement.");

    var branchesBuilder = new AresConditionalBuilder(_indentSize, _capabilities);
    configureBranches?.Invoke(branchesBuilder);

    _statements.Add(new IfNode(
      safeCondition,
      thenBranch.Statements,
      branchesBuilder.ElifBranches,
      branchesBuilder.ElseBranch));
    return this;
  }

  public AresScriptBlockBuilder AddWhile(string conditionExpression, Action<AresScriptBlockBuilder> configureBody)
  {
    var safeCondition = EnsureText(conditionExpression, nameof(conditionExpression));
    ArgumentNullException.ThrowIfNull(configureBody);

    var loopBody = CreateChildBlockBuilder(_capabilities.AllowReturn, allowLoopControl: true);
    configureBody(loopBody);
    EnsureHasStatements(loopBody, "While loop body must contain at least one statement.");

    _statements.Add(new BlockNode($"while {safeCondition}", loopBody.Statements));
    return this;
  }

  public AresScriptBlockBuilder AddFor(string iteratorName, string iterableExpression, Action<AresScriptBlockBuilder> configureBody)
  {
    var safeIterator = ValidateIdentifier(iteratorName, nameof(iteratorName));
    var safeIterable = EnsureText(iterableExpression, nameof(iterableExpression));
    ArgumentNullException.ThrowIfNull(configureBody);

    var loopBody = CreateChildBlockBuilder(_capabilities.AllowReturn, allowLoopControl: true);
    configureBody(loopBody);
    EnsureHasStatements(loopBody, "For loop body must contain at least one statement.");

    _statements.Add(new BlockNode($"for {safeIterator} in {safeIterable}", loopBody.Statements));
    return this;
  }

  public AresScriptBlockBuilder AddParallel(Action<AresParallelBlockBuilder> configureBlock)
  {
    ArgumentNullException.ThrowIfNull(configureBlock);

    var parallel = new AresParallelBlockBuilder();
    configureBlock(parallel);
    if(parallel.Expressions.Count == 0)
    {
      throw new InvalidOperationException("Parallel block must contain at least one expression.");
    }

    _statements.Add(new ParallelNode(parallel.Expressions));
    return this;
  }

  private AresScriptBlockBuilder CreateChildBlockBuilder(bool allowReturn, bool allowLoopControl)
  {
    return new AresScriptBlockBuilder([], _indentSize, new ScriptBuilderCapabilities(allowReturn, allowLoopControl));
  }

  private static void EnsureHasStatements(AresScriptBlockBuilder block, string errorMessage)
  {
    if(block.Statements.Count == 0)
    {
      throw new InvalidOperationException(errorMessage);
    }
  }

  private static string EnsureText(string value, string parameterName)
  {
    return string.IsNullOrWhiteSpace(value) 
      ? throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName) 
      : value.Trim();

  }

  private static string ValidateIdentifier(string identifier, string parameterName)
  {
    var safeIdentifier = EnsureText(identifier, parameterName);
    if(!_identifierRegex.IsMatch(safeIdentifier))
    {
      throw new ArgumentException($"'{safeIdentifier}' is not a valid AresScript identifier.", parameterName);
    }

    return safeIdentifier;
  }
}
