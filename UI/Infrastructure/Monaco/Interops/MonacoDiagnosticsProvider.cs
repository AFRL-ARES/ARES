using Ares.Services;
using Microsoft.JSInterop;
using UI.Application.Scripting;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoDiagnosticsProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient) : IMonacoDiagnosticsProvider
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

}

