namespace Ares.Core.Execution.StartConditions;

public record StartConditionResult(bool Success, IEnumerable<string> Messages)
{

  public StartConditionResult(bool success, string? message = null) : this(success, message is not null ? new[] { message } : Array.Empty<string>())
  {
  }
}
