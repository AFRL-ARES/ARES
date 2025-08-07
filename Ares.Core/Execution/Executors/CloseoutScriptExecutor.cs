using System.Reactive.Linq;
using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

public class CloseoutScriptExecutor : IExecutor<Empty, CampaignCloseoutStatus>
{
  public CloseoutScriptExecutor(ExperimentTemplate template, IExecutor<StepExecutionSummary, StepExecutionStatus>[] closeoutStepExecutors)
  {
    CloseoutStepExecutors = closeoutStepExecutors;
    Template = template;
    Status = new CampaignCloseoutStatus { CampaignId = template.UniqueId };

    Status.CloseoutExecutionStatuses.AddRange(closeoutStepExecutors.Select(executor => executor.Status));

    var experimentStepExecutionObservation = closeoutStepExecutors.Select(executor =>
    {
      return executor.ExperimentStatusObservable.Select(_ =>
      {
        var cmdResults = closeoutStepExecutors.Select(cmdExecutor => cmdExecutor.Status);
        Status.CloseoutExecutionStatuses.Clear();
        Status.CloseoutExecutionStatuses.AddRange(cmdResults);
        return Status;
      });
    }).Concat();

    ExperimentStatusObservable = experimentStepExecutionObservation;
  }

  public IObservable<CampaignCloseoutStatus> ExperimentStatusObservable { get; }

  public IExecutor<StepExecutionSummary, StepExecutionStatus>[] CloseoutStepExecutors { get; }

  public CampaignCloseoutStatus Status { get; }

  public ExperimentTemplate Template { get; set; }


  public async Task<Empty> Execute(ExecutionControlToken executionToken)
  {
    foreach(var closeoutStep in CloseoutStepExecutors)
    {
      if(executionToken.IsCancelled)
        break;

      var stepResult = await closeoutStep.Execute(executionToken);

      if(!stepResult.CommandSummaries.Any())
        break;
    }

    return new Empty();
  }
}
