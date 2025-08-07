using Ares.Core.Execution.StopConditions;
using Ares.Datamodel;

namespace Ares.Core.Execution;

public interface IExecutionManager
{
  /// <summary>
  /// A list of stop conditions for the current campaign (null if no campaign is loaded)
  /// </summary>
  public IList<IStopCondition> CampaignStopConditions { get; }

  /// <summary>
  /// A double value that determines how often a campaign will re-plan it's experiment, defaults to one
  /// </summary>
  public int ReplanRate { get; }

  /// <summary>
  /// Indicates whether the currently loaded campaign has all the prerequisites in order to start and run
  /// </summary>
  public Task<bool> CanRun();

  /// <summary>
  /// Starts the campaign if not already running.
  /// Throws an <see cref="InvalidOperationException" /> if the campaign template has not been set or the execution
  /// prerequisite have not been met
  /// </summary>
  /// <param name="executionNotes"> User notes written for an execution instance, not specific to a template.</param>
  /// <returns>A task that will complete when the campaign completes</returns>
  Task Start(string executionNotes, List<AresCampaignTag> campaignTags);

  /// <summary>
  /// Stops the campaign execution if running or paused. Does nothing if the campaign is not running.
  /// Does not guarantee that the currently running command will immediately stop as that depends on its stopping
  /// implementation
  /// </summary>
  void Stop();

  /// <summary>
  /// Pauses the campaign execution.
  /// Does not guarantee that the currently running command will immediately pause as that depends on its pausing
  /// implementation
  /// </summary>
  void Pause();

  /// <summary>
  /// Resumes the campaign execution if paused, does nothing otherwise.
  /// </summary>
  void Resume();

  /// <summary>
  /// Updates the replan rate of the campaign
  /// </summary>
  /// <param name="newRate"></param>
  void UpdateReplanRate(int newRate);

  /// <summary>
  /// Checks whether the prerequisites to execution have been met
  /// </summary>
  /// <returns> An error string if the campaign is not executable, an empty string otherwise </returns>
  Task<string> CheckCampaignStartPrerequisites();
}
