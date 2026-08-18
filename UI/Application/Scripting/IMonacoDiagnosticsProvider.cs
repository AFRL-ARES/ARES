namespace UI.Application.Scripting;

public interface IMonacoDiagnosticsProvider
{
  Task<MonacoDiagnostic[]> GetDiagnostics(string script);
}

public record MonacoDiagnostic(
  int StartLineNumber,
  int StartColumn,
  int EndLineNumber,
  int EndColumn,
  string Message,
  int Severity,
  string? Code);

