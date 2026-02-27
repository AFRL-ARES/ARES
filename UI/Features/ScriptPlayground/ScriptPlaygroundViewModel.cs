using Ares.Services;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UI.Infrastructure.Grpc;

namespace UI.Features.ScriptPlayground;

public partial class ScriptPlaygroundViewModel : ReactiveObject
{
  private readonly Ares.Core.Grpc.Services.AresScriptingService _scriptingClient;
  private CancellationTokenSource _cancellationTokenSource = new();
  private readonly ISubject<string> _scriptOutput = new Subject<string>();

  public ScriptPlaygroundViewModel(
    Ares.Core.Grpc.Services.AresScriptingService scriptingClient)
  {
    _scriptingClient = scriptingClient;
    ScriptOutput = _scriptOutput.AsObservable();
  }

  public async Task StartScript(string script)
  {
    _cancellationTokenSource = new();
    var token = _cancellationTokenSource.Token;
    ScriptRunning = true;

    var streamWriter = new LocalStreamWriter<ScriptExecutionOutput>(output => 
    {
        _scriptOutput.OnNext(output.Output);
        return Task.CompletedTask;
    });

    try
    {
      await _scriptingClient.ExecuteScript(new ScriptExecutionRequest { Script = script }, streamWriter, null);
    }
    catch(Exception)
    {
    }
    finally
    {
      ScriptRunning = false;
    }
  }

  public async Task StopScript()
  {
    await _cancellationTokenSource.CancelAsync();
  }

  [Reactive]
  public partial bool ScriptRunning { get; private set; }

  public IObservable<string> ScriptOutput { get; }
}
