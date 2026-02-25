using Ares.Datamodel.Scripting;
using Ares.Services;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace UI.Features.ScriptPlayground;

public abstract record ScriptSummaryEvent;

public sealed record SummaryInitializedEvent(IReadOnlyList<ScriptFunctionInvocation> Steps) : ScriptSummaryEvent;

public sealed record SummaryCallStartedEvent(
  long Sequence,
  string CallId,
  string ParentCallId,
  ScriptFunctionInvocation Invocation) : ScriptSummaryEvent;

public sealed record SummaryCallCompletedEvent(string CallId, Ares.Datamodel.AresValue Result) : ScriptSummaryEvent;

public sealed record SummaryCallFailedEvent(string CallId, string Error) : ScriptSummaryEvent;

public partial class ScriptPlaygroundViewModel : ReactiveObject
{
  private readonly AresScriptingService.AresScriptingServiceClient _scriptingClient;
  private CancellationTokenSource _cancellationTokenSource = new();
  private readonly Subject<string> _scriptOutput = new();
  private readonly Subject<ScriptFunctionInvocation> _scriptInvocations = new();
  private readonly Subject<ScriptSummaryEvent> _scriptSummaryEvents = new();

  public ScriptPlaygroundViewModel(
    AresScriptingService.AresScriptingServiceClient scriptingClient)
  {
    _scriptingClient = scriptingClient;
    ScriptOutput = _scriptOutput.AsObservable();
    ScriptInvocations = _scriptInvocations.AsObservable();
    ScriptSummaryEvents = _scriptSummaryEvents.AsObservable();
  }

  public async Task StartScript(string script)
  {
    _cancellationTokenSource = new();
    var token = _cancellationTokenSource.Token;
    ScriptRunning = true;

    await PublishSummaryAsync(script, token);

    var something = _scriptingClient.ExecuteScript(new ScriptExecutionRequest { Script = script }, new CallOptions(cancellationToken: token));

    try
    {
      await foreach(var output in something.ResponseStream.ReadAllAsync(token))
      {
        if(output.FunctionStarted is not null)
        {
          _scriptInvocations.OnNext(output.FunctionStarted.Invocation);
          _scriptSummaryEvents.OnNext(new SummaryCallStartedEvent(
            output.Sequence,
            output.FunctionStarted.CallId,
            output.FunctionStarted.ParentCallId,
            output.FunctionStarted.Invocation));
          continue;
        }

        if(output.FunctionCompleted is not null)
        {
          _scriptSummaryEvents.OnNext(new SummaryCallCompletedEvent(
            output.FunctionCompleted.CallId,
            output.FunctionCompleted.Result));
          continue;
        }

        if(output.FunctionFailed is not null)
        {
          _scriptSummaryEvents.OnNext(new SummaryCallFailedEvent(
            output.FunctionFailed.CallId,
            output.FunctionFailed.Error));
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

  public async Task BuildSummaryAsync(string script)
  {
    await PublishSummaryAsync(script, CancellationToken.None);
  }

  public async Task StopScript()
  {
    await _cancellationTokenSource.CancelAsync();
  }

  private async Task PublishSummaryAsync(string script, CancellationToken cancellationToken)
  {
    var summary = await _scriptingClient.GetScriptSummaryAsync(new ScriptSummaryRequest
    {
      Script = script,
      IncludeUserFunctions = true,
      IncludeLambdas = true
    }, cancellationToken: cancellationToken);
    _scriptSummaryEvents.OnNext(new SummaryInitializedEvent(summary.Steps));

    foreach(var diagnostic in summary.Diagnostics)
    {
      _scriptOutput.OnNext($"Summary diagnostic: {diagnostic.Message} ({diagnostic.StartLine}:{diagnostic.StartColumn})");
    }
  }

  [Reactive]
  public partial bool ScriptRunning { get; private set; }

  public IObservable<string> ScriptOutput { get; }
  public IObservable<ScriptFunctionInvocation> ScriptInvocations { get; }
  public IObservable<ScriptSummaryEvent> ScriptSummaryEvents { get; }
}
