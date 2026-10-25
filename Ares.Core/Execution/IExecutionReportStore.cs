using Ares.Datamodel;

namespace Ares.Core.Execution;

public interface IExecutionReportStore
{
  /// <summary>
  /// Provides status updates for a currently running campaign
  /// </summary>
  IObservable<CampaignExecutionStatus?> CampaignStatusObservable { get; }

  /// <summary>
  /// Provides status updates for a currently running experiment
  /// </summary>
  IObservable<ExperimentExecutionStatus?> ExperimentStatusObservable { get; }

  /// <summary>
  /// Provides completed experiment summaries after analysis and visualization have completed.
  /// </summary>
  IObservable<ExperimentExecutionSummary?> CompletedExperimentObservable { get; }

  /// <summary>
  /// The current campaign execution status
  /// </summary>
  CampaignExecutionStatus? CampaignExecutionStatus { get; set; }

  /// <summary>
  /// The current experiment execution status
  /// </summary>
  ExperimentExecutionStatus? ExperimentExecutionStatus { get; set; }

  /// <summary>
  /// The most recently completed experiment summary.
  /// </summary>
  ExperimentExecutionSummary? LatestCompletedExperiment { get; set; }
}