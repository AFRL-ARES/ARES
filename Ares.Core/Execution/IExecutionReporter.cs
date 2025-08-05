using Ares.Messaging;

namespace Ares.Core.Execution;

public interface IExecutionReporter
{
  /// <summary>
  /// Used internally to take in a campaign execution status and store it into some public object
  /// </summary>
  /// <param name="status"></param>
  void Report(CampaignExecutionStatus status);

  /// <summary>
  /// Used internally to take in a experiment execution status and store it into some public object
  /// </summary>
  /// <param name="status"></param>
  void Report(ExperimentExecutionStatus status);

  /// <summary>
  /// Used to internally take in a campaign startup status and store it into some public object
  /// </summary>
  /// <param name="status"></param>
  void Report(CampaignStartupStatus status);

  /// <summary>
  /// Used to internally take in a campaign closeout status and store it into some public object
  /// </summary>
  /// <param name="status"></param>
  void Report(CampaignCloseoutStatus status);
}
