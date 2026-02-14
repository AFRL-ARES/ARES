using System.Text;

namespace AresScript.ScriptBuilding;

internal readonly record struct ScriptBuilderCapabilities(bool AllowReturn, bool AllowLoopControl)
{
  public static ScriptBuilderCapabilities Root { get; } = new(AllowReturn: false, AllowLoopControl: false);
}

internal abstract class ScriptNode
{
  public abstract void WriteTo(StringBuilder output, int indentLevel, int indentSize);

  protected static void WriteIndent(StringBuilder output, int indentLevel, int indentSize)
  {
    output.Append(' ', indentLevel * indentSize);
  }
}

internal sealed class LineNode(string text) : ScriptNode
{
  public override void WriteTo(StringBuilder output, int indentLevel, int indentSize)
  {
    WriteIndent(output, indentLevel, indentSize);
    output.AppendLine(text);
  }
}

internal sealed class BlockNode(string header, IReadOnlyList<ScriptNode> statements) : ScriptNode
{
  public override void WriteTo(StringBuilder output, int indentLevel, int indentSize)
  {
    WriteIndent(output, indentLevel, indentSize);
    output.Append(header);
    output.AppendLine(":");
    foreach(ScriptNode statement in statements)
    {
      statement.WriteTo(output, indentLevel + 1, indentSize);
    }
  }
}

internal sealed class ConditionalBranchNode(string header, IReadOnlyList<ScriptNode> statements)
{
  public string Header { get; } = header;

  public IReadOnlyList<ScriptNode> Statements { get; } = statements;
}

internal sealed class IfNode(
  string condition,
  IReadOnlyList<ScriptNode> thenStatements,
  IReadOnlyList<ConditionalBranchNode> elifBranches,
  IReadOnlyList<ScriptNode>? elseBranch) : ScriptNode
{

  public override void WriteTo(StringBuilder output, int indentLevel, int indentSize)
  {
    WriteBranch(output, indentLevel, indentSize, $"if {condition}", thenStatements);
    foreach(ConditionalBranchNode elifBranch in elifBranches)
    {
      WriteBranch(output, indentLevel, indentSize, elifBranch.Header, elifBranch.Statements);
    }

    if(elseBranch is not null)
    {
      WriteBranch(output, indentLevel, indentSize, "else", elseBranch);
    }
  }

  private static void WriteBranch(
    StringBuilder output,
    int indentLevel,
    int indentSize,
    string branchHeader,
    IReadOnlyList<ScriptNode> statements)
  {
    WriteIndent(output, indentLevel, indentSize);
    output.Append(branchHeader);
    output.AppendLine(":");
    foreach(ScriptNode statement in statements)
    {
      statement.WriteTo(output, indentLevel + 1, indentSize);
    }
  }
}

internal sealed class ParallelNode(IReadOnlyList<string> expressions) : ScriptNode
{

  public override void WriteTo(StringBuilder output, int indentLevel, int indentSize)
  {
    WriteIndent(output, indentLevel, indentSize);
    output.AppendLine("parallel:");
    foreach(string expression in expressions)
    {
      WriteIndent(output, indentLevel + 1, indentSize);
      output.AppendLine(expression);
    }
  }
}
