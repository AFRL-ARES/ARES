namespace AresScript.ScriptBuilding;

public sealed class AresParallelBlockBuilder
{
  private readonly List<string> _expressions = [];

  internal IReadOnlyList<string> Expressions => _expressions;

  public AresParallelBlockBuilder AddExpression(string expression)
  {
    if(string.IsNullOrWhiteSpace(expression))
    {
      throw new ArgumentException("Expression cannot be null, empty, or whitespace.", nameof(expression));
    }

    _expressions.Add(expression.Trim());
    return this;
  }
}
