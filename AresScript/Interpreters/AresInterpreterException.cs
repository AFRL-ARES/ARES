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
    : base($"{message} (Line {line}, Column {column})")
  {
    Line = line;
    Column = column;
    DetailMessage = message;
  }

  public AresInterpreterException(string message, int line, int column, Exception? innerException)
    : base($"{message} (Line {line}, Column {column})", innerException)
  {
    Line = line;
    Column = column;
    DetailMessage = message;
  }
}
