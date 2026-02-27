using Ares.Services;
using Ares.Core.Grpc.Services;
using Microsoft.JSInterop;
using UI.Application.Scripting;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoDiagnosticsProvider(Ares.Core.Grpc.Services.AresScriptingService aresScriptingServiceClient) : IMonacoDiagnosticsProvider
{
  private readonly Ares.Core.Grpc.Services.AresScriptingService _aresScriptingServiceClient = aresScriptingServiceClient;

  [JSInvokable]
  public async Task<MonacoDiagnostic[]> GetDiagnostics(string script)
  {
    var response = await _aresScriptingServiceClient.ValidateScript(new ValidateScriptRequest
    {
      Script = script ?? string.Empty
    }, null);

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

