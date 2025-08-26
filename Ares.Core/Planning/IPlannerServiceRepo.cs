namespace Ares.Core.Planning;

public interface IPlannerServiceRepo
{
  IEnumerable<IPlannerService> AvailablePlannerServices { get; }

  /// <summary>
  /// Gets the default "None" planner
  /// </summary>
  /// <returns>The None Planner</returns>
  IPlannerService GetDefaultPlanner();

  /// <summary>
  /// Gets the manual planner
  /// </summary>
  /// <returns>The Manual Planner</returns>
  ManualPlanner GetManualPlanner();

  /// <summary>
  /// Gets a named planner based on the given planner name/> object
  /// </summary>
  /// <param name="name">The name of the planner requested</param>
  /// <returns>The planner or null if none is found </returns>
  IPlannerService? GetPlannerByName(string name);

  /// <summary>
  /// Gets a named planner based on the given planner id/> object
  /// </summary>
  /// <param name="id">The id of the planner requested</param>
  /// <returns>The planner or null if none is found </returns>
  IPlannerService? GetPlannerById(string id);

  /// <summary>
  /// Adds an planner to the registry so that it can later be used by experiment execution
  /// </summary>
  /// <param name="planner">The planner to register</param>
  internal void AddPlanner(IPlannerService planner);

  /// <summary>
  /// Removes an planner from the registry
  /// </summary>
  /// <param name="planner">The planner to remove</param>
  internal void RemovePlanner(IPlannerService planner);

  /// <summary>
  /// Removes an planner from the registry based on the id
  /// </summary>
  /// <param name="plannerId">The id of the planner to be removed</param>
  internal void RemovePlanner(string plannerId);
}
