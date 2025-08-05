namespace Ares.Core.Planning;

public interface IPlannerManager
{
  /// <summary>
  /// Gets a planner from the registry
  /// </summary>
  /// <typeparam name="T">Type of planner that implements IPlanner</typeparam>
  /// <returns>Planner of the given type</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  T GetPlanner<T>() where T : IPlanner;

  /// <summary>
  /// Gets a planner with a specific version from the registry
  /// </summary>
  /// <param name="version">Specific version of the planner type to get</param>
  /// <typeparam name="T">Type of planner that implements IPlanner</typeparam>
  /// <returns>Planner of the given type and version</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  T GetPlanner<T>(Version version) where T : IPlanner;

  /// <summary>
  /// Gets a named planner with a specific version from the registry
  /// </summary>
  /// <param name="name">Name of the planner</param>
  /// <param name="version">Specific version of the planner type to get</param>
  /// <typeparam name="T">Type of planner that implements IPlanner</typeparam>
  /// <returns>Planner of the given type, version, and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  T GetPlanner<T>(string name, Version version) where T : IPlanner;

  /// <summary>
  /// Gets a planner from the registry
  /// </summary>
  /// <param name="type">The type name of the planner</param>
  /// <returns>Planner of the given type</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  IPlanner GetPlanner(string type);

  /// <summary>
  /// Gets a planner with a specific version from the registry
  /// </summary>
  /// <param name="type">The type name of the planner</param>
  /// <param name="version">Specific version of the planner type to get</param>
  /// <returns>Planner of the given type and version</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  IPlanner GetPlanner(string type, Version version);

  /// <summary>
  /// Gets a named planner with a specific version from the registry
  /// </summary>
  /// <param name="type">The type name of the planner</param>
  /// <param name="name">Name of the planner</param>
  /// <param name="version">Specific version of the planner type to get</param>
  /// <returns>Planner of the given type, version, and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  IPlanner GetPlanner(string type, string name, Version version);

  /// <summary>
  /// Gets a named planner of the latest version from the registry
  /// </summary>
  /// <param name="type">The type name of the planner</param>
  /// <param name="name">Name of the planner</param>
  /// <returns>Planner of the given type and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the planner is not found</exception>
  IPlanner GetPlanner(string type, string name);

  /// <summary>
  /// Gets the first planner available matching a given name.
  /// </summary>
  /// <param name="name"></param>
  /// <returns>Planner of the given name</returns>
  IPlanner? GetPlannerByName(string name);

  /// <summary>
  /// Registers a planner with the planner manager.
  /// </summary>
  /// <param name="planner">The planner to be registered.</param>
  /// <returns></returns>
  Task RegisterPlanner(IPlanner planner);

  /// <summary>
  /// Unregisters a planner with the planner manager.
  /// </summary>
  /// <param name="planner">Planner to be unregistered.</param>
  /// <returns></returns>
  Task UnregisterPlanner(IPlanner planner);

  /// <summary>
  /// Initializes the planner manager, loading all stored planners from the database
  /// </summary>
  /// <returns></returns>
  Task Init();

  /// <summary>
  /// The list of planners currently registers with the planner manager.
  /// </summary>
  IEnumerable<IPlanner> AvailablePlanners { get; }
}