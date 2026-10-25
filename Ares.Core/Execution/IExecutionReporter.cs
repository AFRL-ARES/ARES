using Ares.Datamodel;

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
  /// Used internally to publish a completed experiment summary after all experiment phases finish.
  /// </summary>
  /// <param name="summary"></param>
  void Report(ExperimentExecutionSummary summary);
}