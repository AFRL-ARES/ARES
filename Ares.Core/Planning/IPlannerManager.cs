using DynamicData;

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
  void RegisterPlanner(IPlanner planner);
  IEnumerable<IPlanner> AvailablePlanners { get; }
}