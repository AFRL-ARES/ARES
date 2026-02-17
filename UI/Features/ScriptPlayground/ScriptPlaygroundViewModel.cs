using Ares.Datamodel.Scripting;
using Ares.Services;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace UI.Features.ScriptPlayground;

public partial class ScriptPlaygroundViewModel : ReactiveObject
{
  private readonly AresScriptingService.AresScriptingServiceClient _scriptingClient;
  private CancellationTokenSource _cancellationTokenSource = new();
  private readonly Subject<string> _scriptOutput = new();
  private readonly Subject<ScriptFunctionInvocation> _scriptInvocations = new();

  public ScriptPlaygroundViewModel(
    AresScriptingService.AresScriptingServiceClient scriptingClient)
  {
    _scriptingClient = scriptingClient;
    ScriptOutput = _scriptOutput.AsObservable();
    ScriptInvocations = _scriptInvocations.AsObservable();
  }

  public async Task StartScript(string script)
  {
    _cancellationTokenSource = new();
    var token = _cancellationTokenSource.Token;
    ScriptRunning = true;
    var something = _scriptingClient.ExecuteScript(new ScriptExecutionRequest { Script = script }, new CallOptions(cancellationToken: token));

    try
    {
      await foreach(var output in something.ResponseStream.ReadAllAsync(token))
      {
        if(output.FunctionStarted is not null)
        {
          _scriptInvocations.OnNext(output.FunctionStarted.Invocation);
          continue;
        }

        if(output.ConsoleOutput is not null && !string.IsNullOrEmpty(output.ConsoleOutput.Output))
        {
          _scriptOutput.OnNext(output.ConsoleOutput.Output);
          continue;
        }

        if(output.ExecutionFailed is not null && !string.IsNullOrEmpty(output.ExecutionFailed.Error))
        {
          _scriptOutput.OnNext($"Run failed: {output.ExecutionFailed.Error}");
        }
      }
    }
    catch(RpcException)
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
  public IObservable<ScriptFunctionInvocation> ScriptInvocations { get; }
}
