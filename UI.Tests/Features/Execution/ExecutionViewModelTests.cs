using Ares.Datamodel;
using ReactiveUI.Builder;
using UI.Domain.Execution;
using UI.Features.Execution;

namespace UI.Tests.Features.Execution;

internal class ExecutionViewModelTests
{
  [OneTimeSetUp]
  public void InitializeReactiveUi()
  {
    RxAppBuilder
      .CreateReactiveUIBuilder()
      .WithCoreServices()
      .BuildApp();
  }

  [Test]
  public void ApplyCampaignExecutionStatus_RestoresExperimentHistoryInCampaignOrder()
  {
    var startup = new ExperimentExecutionStatus { ExperimentId = "startup", IsStartup = true };
    var firstExperiment = new ExperimentExecutionStatus { ExperimentId = "experiment-1" };
    var secondExperiment = new ExperimentExecutionStatus { ExperimentId = "experiment-2" };
    var status = new CampaignExecutionStatus
    {
      CampaignId = "campaign",
      State = ExecutionState.Running
    };
    status.ExperimentExecutionStatuses.Add(startup);
    status.ExperimentExecutionStatuses.Add(firstExperiment);
    status.ExperimentExecutionStatuses.Add(secondExperiment);
    var viewModel = new ExecutionViewModel(null!, null!, null!, null!, null!, null!);

    viewModel.ApplyCampaignExecutionStatus(status);

    Assert.That(viewModel.ExperimentExecutionStatuses.Select(experiment => experiment.ExperimentId),
      Is.EqualTo(new[] { "startup", "experiment-1", "experiment-2" }));
    Assert.That(viewModel.CampaignExecutionState, Is.EqualTo(ExecutionState.Running));
    Assert.That(viewModel.CampaignActive, Is.True);
  }

  [Test]
  public void ApplyCampaignExecutionStatus_DoesNotModifyPreviouslyPublishedSnapshot()
  {
    var firstStatus = new CampaignExecutionStatus { State = ExecutionState.Running };
    firstStatus.ExperimentExecutionStatuses.Add(new ExperimentExecutionStatus { ExperimentId = "experiment-1" });
    var secondStatus = new CampaignExecutionStatus { State = ExecutionState.Running };
    secondStatus.ExperimentExecutionStatuses.Add(new ExperimentExecutionStatus { ExperimentId = "experiment-1" });
    secondStatus.ExperimentExecutionStatuses.Add(new ExperimentExecutionStatus { ExperimentId = "experiment-2" });
    var viewModel = new ExecutionViewModel(null!, null!, null!, null!, null!, null!);
    viewModel.ApplyCampaignExecutionStatus(firstStatus);
    var firstSnapshot = viewModel.ExperimentExecutionStatuses;

    viewModel.ApplyCampaignExecutionStatus(secondStatus);

    Assert.That(firstSnapshot.Select(experiment => experiment.ExperimentId), Is.EqualTo(new[] { "experiment-1" }));
    Assert.That(viewModel.ExperimentExecutionStatuses.Select(experiment => experiment.ExperimentId),
      Is.EqualTo(new[] { "experiment-1", "experiment-2" }));
  }

  [Test]
  public void GetExperimentNumber_IgnoresStartupAndCloseoutStatuses()
  {
    var statuses = new[]
    {
      new ExperimentExecutionStatus { IsStartup = true },
      new ExperimentExecutionStatus(),
      new ExperimentExecutionStatus(),
      new ExperimentExecutionStatus { IsCloseout = true }
    };

    Assert.That(statuses.GetExperimentNumber(1), Is.EqualTo(1));
    Assert.That(statuses.GetExperimentNumber(2), Is.EqualTo(2));
  }

  [Test]
  public void GetExperimentNumber_StartsAtOneWhenThereIsNoStartupStatus()
  {
    var statuses = new[]
    {
      new ExperimentExecutionStatus(),
      new ExperimentExecutionStatus()
    };

    Assert.That(statuses.GetExperimentNumber(0), Is.EqualTo(1));
    Assert.That(statuses.GetExperimentNumber(1), Is.EqualTo(2));
  }
}
