namespace AresScript.ScriptBuilding;

public sealed class AresConditionalBuilder
{
  private readonly int _indentSize;
  private readonly ScriptBuilderCapabilities _capabilities;
  private readonly List<ConditionalBranchNode> _elifBranches = [];
  private IReadOnlyList<ScriptNode>? _elseBranch;

  internal AresConditionalBuilder(int indentSize, ScriptBuilderCapabilities capabilities)
  {
    _indentSize = indentSize;
    _capabilities = capabilities;
  }

  internal IReadOnlyList<ConditionalBranchNode> ElifBranches => _elifBranches;

  internal IReadOnlyList<ScriptNode>? ElseBranch => _elseBranch;

  public AresConditionalBuilder AddElif(string conditionExpression, Action<AresScriptBlockBuilder> configureBranch)
  {
    var safeCondition = EnsureText(conditionExpression, nameof(conditionExpression));
    ArgumentNullException.ThrowIfNull(configureBranch);

    var branch = new AresScriptBlockBuilder([], _indentSize, _capabilities);
    configureBranch(branch);
    EnsureHasStatements(branch, "Elif branch must contain at least one statement.");

    _elifBranches.Add(new ConditionalBranchNode($"elif {safeCondition}", branch.Statements));
    return this;
  }

  public AresConditionalBuilder AddElse(Action<AresScriptBlockBuilder> configureBranch)
  {
    ArgumentNullException.ThrowIfNull(configureBranch);
    if(_elseBranch is not null)
    {
      throw new InvalidOperationException("Else branch has already been configured.");
    }

    var branch = new AresScriptBlockBuilder([], _indentSize, _capabilities);
    configureBranch(branch);
    EnsureHasStatements(branch, "Else branch must contain at least one statement.");

    _elseBranch = branch.Statements;
    return this;
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
}
