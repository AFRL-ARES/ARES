using Ares.Core.Execution.StopConditions;

namespace Ares.Core.Execution;

public interface IExecutionManager
{
  /// <summary>
  /// A list of stop conditions for the current campaign (null if no campaign is loaded)
  /// </summary>
  public IList<IStopCondition>? CampaignStopConditions { get; }

  /// <summary>
  /// Indicates whether the currently loaded campaign has all the prerequisites in order to start and run
  /// </summary>
  public bool CanRun { get; }

  /// <summary>
  /// Starts the campaign if not already running.
  /// Throws an <see cref="InvalidOperationException" /> if the campaign template has not been set or the execution
  /// prerequisite have not been met
  /// </summary>
  /// <returns>A task that will complete when the campaign completes</returns>
  Task Start();

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
}
