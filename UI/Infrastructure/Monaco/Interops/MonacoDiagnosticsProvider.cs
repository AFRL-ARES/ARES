using Ares.Services;
using Microsoft.JSInterop;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoDiagnosticsProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient)
{
  private readonly AresScriptingService.AresScriptingServiceClient _aresScriptingServiceClient = aresScriptingServiceClient;

  [JSInvokable]
  public async Task<MonacoDiagnostic[]> GetDiagnostics(string script)
  {
    var response = await _aresScriptingServiceClient.ValidateScriptAsync(new ValidateScriptRequest
    {
      Script = script ?? string.Empty
    });

    return response.Diagnostics.Select(d => new MonacoDiagnostic(
      d.StartLine,
      d.StartColumn,
      d.EndLine,
      d.EndColumn,
      d.Message,
      (int)d.Severity,
      d.Code
    )).ToArray();
  }

  public record MonacoDiagnostic(
    int StartLineNumber,
    int StartColumn,
    int EndLineNumber,
    int EndColumn,
    string Message,
    int Severity,
    string? Code);
}
