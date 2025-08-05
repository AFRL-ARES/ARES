using Ares.Messaging;

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
  /// Provides status updates for a currently running startup script
  /// </summary>
  IObservable<CampaignStartupStatus?> CampaignStartupStatusObservable { get; }
  
  /// <summary>
  /// Provides status updates for a currently running closeout script
  /// </summary>
  IObservable<CampaignCloseoutStatus?> CampaignCloseoutStatusObservable { get; }

  /// <summary>
  /// The current campaign execution status
  /// </summary>
  CampaignExecutionStatus? CampaignExecutionStatus { get; set; }

  /// <summary>
  /// The current experiment execution status
  /// </summary>
  ExperimentExecutionStatus? ExperimentExecutionStatus { get; set; }

  /// <summary>
  /// The current campaign startup status
  /// </summary>
  CampaignStartupStatus? CampaignStartupStatus { get; set; }

  /// <summary>
  /// The current campaign closeout status
  /// </summary>
  CampaignCloseoutStatus? CampaignCloseoutStatus { get; set; }
}
