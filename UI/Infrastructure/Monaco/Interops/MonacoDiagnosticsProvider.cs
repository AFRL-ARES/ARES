using Ares.Services;
using Microsoft.JSInterop;
using UI.Application.Scripting;
using ScriptingService = Ares.Core.Grpc.Services.AresScriptingService;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoDiagnosticsProvider(ScriptingService scriptingService) : IMonacoDiagnosticsProvider
{
  private readonly ScriptingService _scriptingService = scriptingService;

  [JSInvokable]
  public async Task<MonacoDiagnostic[]> GetDiagnostics(string script)
  {
    var response = await _scriptingService.ValidateScript(new ValidateScriptRequest
    {
      Script = script ?? string.Empty
    }, null!);

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

