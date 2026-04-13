using Ares.Core.Execution.Extensions;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using AresScript;
using AresScript.Environment;
using AresScript.ScriptAnalysis;
using AresScript.Symbols;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Execution.Executors;

public class ExperimentExecutor
{
  private readonly BaseEnvironmentBuilder _environmentBuilder;
  private readonly Subject<string> _scriptOutputSubject = new();
  private readonly Subject<ScriptExecutionEvent> _scriptEventSubject = new();
  private readonly BehaviorSubject<ExperimentExecutionStatus> _statusSubject;

  public ExperimentExecutor(ExperimentTemplate template, BaseEnvironmentBuilder environmentBuilder)
  {
    Template = template;
    _environmentBuilder = environmentBuilder;

    Status = new ExperimentExecutionStatus
    {
      ExperimentId = template.UniqueId
    };

    _statusSubject = new BehaviorSubject<ExperimentExecutionStatus>(Status);
    ExperimentStatusObservable = _statusSubject.AsObservable();
    ScriptOutput = _scriptOutputSubject.AsObservable();
    ScriptEvents = _scriptEventSubject.AsObservable();
  }

  public async Task<ExperimentExecutionSummary> Execute(ScriptExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    using var controlTokenSource = new ScriptExecutionControlTokenSource(token.CancellationToken);

    var environment = BuildEnvironment(controlTokenSource);
    var runner = new ScriptRunner(environment, captureExecutionEventsWithoutSubscribers: true);

    using var outputSubscription = runner.ScriptOutput.Subscribe(_scriptOutputSubject);
    using var eventSubscription = runner.ScriptEvents.Subscribe(scriptEvent =>
    {
      _scriptEventSubject.OnNext(scriptEvent);
      UpdateStatus(scriptEvent);
    });

    UpdateStatus(ExecutionState.Running);

    try
    {
      await runner.RunScriptAsync(Template.Script, controlTokenSource.Token);

      var completedExperiment = await PopulateExperimentSummary(environment);
      var endTime = DateTime.UtcNow;
      var stepSummaries = new[] { CreateScriptStepSummary(startTime, endTime, true, null) };
      return ExecutorSummaryHelpers.CreateExperimentExecutionSummary(completedExperiment, startTime, endTime, stepSummaries);
    }
    catch(Exception ex)
    {
      UpdateStatus(ExecutionState.Failed);

      var completedExperiment = await PopulateExperimentSummary(environment, ex);
      var endTime = DateTime.UtcNow;
      var stepSummaries = new[] { CreateScriptStepSummary(startTime, endTime, false, ex) };
      return ExecutorSummaryHelpers.CreateExperimentExecutionSummary(completedExperiment, startTime, endTime, stepSummaries);
    }
  }

  public Task<ExperimentOverview> PopulateExperimentSummary(AresScriptEnvironment environment, Exception? executionError = null)
  {
    var completedExperiment = new ExperimentOverview
    {
      UniqueId = Guid.NewGuid().ToString(),
      Template = Template.Clone(),
      Result = executionError is null ? ResultGenerator.GetExperimentResult(environment) : AresValueHelper.CreateUnit(),
    };

    completedExperiment.Parameters.AddRange(Template.GetAllParameters().Select(parameter => parameter.Clone()));
    return Task.FromResult(completedExperiment);
  }

  public ExperimentTemplate Template { get; set; }

  public IObservable<ExperimentExecutionStatus> ExperimentStatusObservable { get; }
  public IObservable<string> ScriptOutput { get; }
  public IObservable<ScriptExecutionEvent> ScriptEvents { get; }

  public ExperimentExecutionStatus Status { get; }

  private AresScriptEnvironment BuildEnvironment(ScriptExecutionControlTokenSource tokenSource)
  {
    var environment = _environmentBuilder.Build();

    foreach(var parameter in Template.GetAllParameters())
    {
      if(parameter.Metadata is null || parameter.Value is null || string.IsNullOrWhiteSpace(parameter.Metadata.Name))
      {
        continue;
      }

      environment.AssignVariable(parameter.Metadata.Name, parameter.Value, parameter.Metadata.Schema);
    }

    environment.AddSystemFunction(ExperimentSymbolCreator.CreateFail());
    environment.AddSystemFunction(ExperimentSymbolCreator.CreatePause(tokenSource));
    environment.AddSystemFunction(ExperimentSymbolCreator.CreateStop(tokenSource));

    return environment;
  }

  private StepExecutionSummary CreateScriptStepSummary(DateTime startTime, DateTime endTime, bool success, Exception? error)
  {
    var commandSummary = new CommandExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      CommandId = Template.UniqueId,
      CommandName = string.IsNullOrWhiteSpace(Template.Name) ? "experiment_script" : Template.Name,
      CommandDescription = "ARES experiment script execution",
      ExecutionInfo = CreateExecutionInfo(startTime, endTime),
      Result = new CommandResult
      {
        UniqueId = Guid.NewGuid().ToString(),
        Success = success,
        Error = error?.Message ?? string.Empty
      }
    };

    return new StepExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      StepId = Template.UniqueId,
      ExecutionInfo = CreateExecutionInfo(startTime, endTime),
      CommandSummaries = { commandSummary }
    };
  }

  private static ExecutionInfo CreateExecutionInfo(DateTime startTime, DateTime endTime)
  {
    return new ExecutionInfo
    {
      UniqueId = Guid.NewGuid().ToString(),
      TimeStarted = Timestamp.FromDateTime(startTime.ToUniversalTime()),
      TimeFinished = Timestamp.FromDateTime(endTime.ToUniversalTime()),
      Timezone = TimeZoneInfo.Local.DisplayName,
      LocaltimeOffset = DateTimeOffset.Now.Offset.ToString()
    };
  }

  private void UpdateStatus(ScriptExecutionEvent scriptEvent)
  {
    switch(scriptEvent)
    {
      case ScriptExecutionStartedEvent:
        UpdateStatus(ExecutionState.Running);
        break;
      case ScriptExecutionCompletedEvent:
        UpdateStatus(ExecutionState.Succeeded);
        break;
      case ScriptExecutionFailedEvent:
        UpdateStatus(ExecutionState.Failed);
        break;
    }
  }

  private void UpdateStatus(ExecutionState state)
  {
    _statusSubject.OnNext(Status);
  }
}
