namespace AresScript.Interpreters;

public sealed class AresInterpreterException : InvalidOperationException
{
  public int Line { get; }
  public int Column { get; }
  public string DetailMessage { get; }

  public AresInterpreterException(string message)
    : base(message)
  {
    DetailMessage = message;
  }

  public AresInterpreterException(string message, int line, int column)
    : base($"{message} (Line {line}, Column {NormalizeColumn(column)})")
  {
    Line = line;
    Column = NormalizeColumn(column);
    DetailMessage = message;
  }

  public AresInterpreterException(string message, int line, int column, Exception? innerException)
    : base($"{message} (Line {line}, Column {NormalizeColumn(column)})", innerException)
  {
    Line = line;
    Column = NormalizeColumn(column);
    DetailMessage = message;
  }

  private static int NormalizeColumn(int column) => Math.Max(1, column + 1);
}
