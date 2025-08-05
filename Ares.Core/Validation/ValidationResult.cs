namespace Ares.Core.Validation;

public record ValidationResult
{
  public bool Success { get; init; }
  public IEnumerable<string> Messages { get; init; }
  public ValidationResult(bool success, string? message = null)
  {
    Success = success;
    Messages = message is not null ? [message] : Array.Empty<string>();
  }

  public ValidationResult(bool success, IEnumerable<string> messages)
  {
    Success = success;
    Messages = messages.ToArray();
  }

  public ValidationResult(IEnumerable<ValidationResult> results)
  {
    var resultsArr = results.ToArray();
    Success = resultsArr.All(r => r.Success);
    Messages = resultsArr.SelectMany(r => r.Messages);
  }
}
