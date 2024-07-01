using Ares.Messaging;

namespace Ares.Core.Planning;

public interface IPlanner
{
  /// <summary>
  /// Optional name for the planner (can be useful when multiple planners of same type and version have to be used)
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Version of the planner
  /// </summary>
  Version Version { get; }

  /// <summary>
  /// Current state (<see cref="PlannerState" />) of the planner which essentially indicated
  /// whether or not this planner is currently available for planning
  /// </summary>
  IObservable<PlannerState> PlannerState { get; }

  /// <summary>
  /// Returns the values for the given parameter metadata
  /// </summary>
  /// <param name="plannableParameters">Collection of parameter metadata to plan for</param>
  /// <param name="experimentAnalyses">The experiment results to use as a seed for planning</param>
  /// <returns>Collection of plan <see cref="PlanResult" /> which has the metadata and the value</returns>
  Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<Analysis> experimentAnalyses, CancellationToken cancellationToken);
}
