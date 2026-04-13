using Ares.Core.Scripting;
using Ares.Datamodel.Scripting;
using Ares.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ScriptingService = Ares.Core.Grpc.Services.AresScriptingService;

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
  private readonly ScriptingService _scriptingService;
  private CancellationTokenSource _cancellationTokenSource = new();
  private readonly Subject<string> _scriptOutput = new();
  private readonly Subject<ScriptSummaryEvent> _scriptSummaryEvents = new();

  public ScriptPlaygroundViewModel(
    ScriptingService scriptingService)
  {
    _scriptingService = scriptingService;
    ScriptOutput = _scriptOutput.AsObservable();
    ScriptSummaryEvents = _scriptSummaryEvents.AsObservable();
  }

  public async Task StartScript(string script)
  {
    _cancellationTokenSource = new();
    var token = _cancellationTokenSource.Token;
    ScriptRunning = true;

    await PublishSummaryAsync(script, token);

    try
    {
      await foreach(var output in _scriptingService.ExecuteScript(new ScriptExecutionRequest { Script = script }, token))
      {
        if(output is ScriptFunctionStartedEvent functionStarted)
        {
          _scriptSummaryEvents.OnNext(new SummaryCallStartedEvent(
            functionStarted.Sequence,
            functionStarted.CallId,
            functionStarted.ParentCallId,
            functionStarted.Invocation));
          continue;
        }

        if(output is ScriptFunctionCompletedEvent functionCompleted)
        {
          _scriptSummaryEvents.OnNext(new SummaryCallCompletedEvent(
            functionCompleted.CallId,
            functionCompleted.Result));
          continue;
        }

        if(output is ScriptFunctionFailedEvent functionFailed)
        {
          _scriptSummaryEvents.OnNext(new SummaryCallFailedEvent(
            functionFailed.CallId,
            functionFailed.Error));
          continue;
        }

        if(output is ScriptConsoleOutputEvent consoleOutput && !string.IsNullOrEmpty(consoleOutput.Output))
        {
          _scriptOutput.OnNext(consoleOutput.Output);
          continue;
        }

        if(output is ScriptExecutionFailedEvent executionFailed && !string.IsNullOrEmpty(executionFailed.Error))
        {
          _scriptOutput.OnNext($"Run failed: {executionFailed.Error}");
        }
      }
    }
    catch(OperationCanceledException) when(token.IsCancellationRequested)
    {
    }
    catch(Exception)
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
    var summary = await _scriptingService.GetScriptSummary(new ScriptSummaryRequest
    {
      Script = script,
      IncludeUserFunctions = true,
      IncludeLambdas = true
    }, null!);
    _scriptSummaryEvents.OnNext(new SummaryInitializedEvent(summary.Steps));

    foreach(var diagnostic in summary.Diagnostics)
    {
      _scriptOutput.OnNext($"Summary diagnostic: {diagnostic.Message} ({diagnostic.StartLine}:{diagnostic.StartColumn})");
    }
  }

  [Reactive]
  public partial bool ScriptRunning { get; private set; }

  public IObservable<string> ScriptOutput { get; }
  public IObservable<ScriptSummaryEvent> ScriptSummaryEvents { get; }
}
