using System.Reactive.Linq;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public class ExperimentExecutor : IExecutor<ExperimentExecutionSummary, ExperimentExecutionStatus>
{
  public ExperimentExecutor(ExperimentTemplate template, IExecutor<StepExecutionSummary, StepExecutionStatus>[] experimentStepExecutors)
  {
    ExperimentStepExecutors = experimentStepExecutors;
    Template = template;

    Status = new ExperimentExecutionStatus
    {
      ExperimentId = template.UniqueId
    };

    Status.StepExecutionStatuses.AddRange(experimentStepExecutors.Select(executor => executor.Status));

    var experimentStepExecutionObservation = experimentStepExecutors.Select(executor =>
    {
      return executor.ExperimentStatusObservable.Select(_ =>
      {
        var cmdResults = experimentStepExecutors.Select(cmdExecutor => cmdExecutor.Status);
        Status.StepExecutionStatuses.Clear();
        Status.StepExecutionStatuses.AddRange(cmdResults);
        return Status;
      });
    }).Concat();

    ExperimentStatusObservable = experimentStepExecutionObservation;
  }

  public async Task<ExperimentExecutionSummary> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var stepSummaries = new List<StepExecutionSummary>();
    foreach(var executableStep in ExperimentStepExecutors)
    {
      if(token.IsCancelled)
        break;

      var stepResult = await executableStep.Execute(token);

      if(!stepResult.CommandSummaries.Any())
        break;

      stepSummaries.Add(stepResult);
    }

    var completedExperiment = await PopulateExperimentSummary(stepSummaries);
    return ExecutorSummaryHelpers.CreateExperimentExecutionSummary(completedExperiment, startTime, DateTime.UtcNow, stepSummaries);
  }

  public Task<ExperimentOverview> PopulateExperimentSummary(List<StepExecutionSummary> stepSummaries)
  {
    var completedExperiment = new ExperimentOverview
    {
      UniqueId = Guid.NewGuid().ToString(),
      Template = Template.AssignNewUniquePlanningIds(),
      Result = ResultGenerator.GenerateExperimentResult(stepSummaries, Template.StepTemplates),
    };

    completedExperiment.Parameters.AddRange(Template.GetAllParameters());
    return Task.FromResult(completedExperiment);
  }

  public IExecutor<StepExecutionSummary, StepExecutionStatus>[] ExperimentStepExecutors { get; }

  public ExperimentTemplate Template { get; set; }

  public IObservable<ExperimentExecutionStatus> ExperimentStatusObservable { get; }

  public ExperimentExecutionStatus Status { get; }
}
