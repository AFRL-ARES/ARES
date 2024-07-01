namespace Ares.Core.Validation;

public record ValidationResult(bool Success, IEnumerable<string> Messages)
{

  public ValidationResult(bool success, string? message = null) : this(success, message is not null ? new[] { message } : Array.Empty<string>())
  {
  }
}
