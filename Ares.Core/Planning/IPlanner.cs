using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

public interface IPlanner
{
  /// <summary>
  /// Optional name for the planner (can be useful when multiple planners of same type and version have to be used)
  /// </summary>
  string Name { get; set; }

  /// <summary>
  /// Version of the planner
  /// </summary>
  Version Version { get; set; }

  /// <summary>
  /// Address used to communicate with the planner
  /// </summary>
  string Address { get; set; }

  /// <summary>
  /// Current status (<see cref="PlannerStatus" />) of the planner which essentially indicates
  /// whether or not this planner is currently available for planning
  /// </summary>
  PlannerStatus Status { get; }

  /// <summary>
  /// A list of planners reported to be available by the planner service
  /// </summary>
  IList<Planner> AvailablePlanners { get; }

  /// <summary>
  /// A list of settings custom to this adapter
  /// </summary>
  IList<PlannerSetting> AdapterSettings { get; }

  /// <summary>
  /// Id used to uniquely identify this instance of the planner
  /// </summary>
  string UniqueId { get; set; }

  /// <summary>
  /// Initializes the planner adapter, including verifying the connection status
  /// </summary>
  /// <returns></returns>
  Task Init();

  /// <summary>
  /// Returns the values for the given parameter metadata
  /// </summary>
  /// <param name="plannableParameters">Collection of parameter metadata to plan for</param>
  /// <param name="analysisHistory">The experiment results to use as a seed for planning</param>
  /// <returns>Collection of plan <see cref="PlanResult" /> which has the metadata and the value</returns>
  Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<CompletedExperiment> previousExperiments, IEnumerable<Analysis> analysisHistory, CancellationToken cancellationToken = default);
}
