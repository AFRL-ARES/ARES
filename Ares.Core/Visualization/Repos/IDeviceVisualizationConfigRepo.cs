using Ares.Datamodel.Visualizing.Local;
using DynamicData;

namespace Ares.Core.Visualization.Repos;

public interface IDeviceVisualizationConfigRepo : IDisposable, IEnumerable<DeviceVisualizationConfig>
{
  /// <summary>
  /// Storage Engine accessors used by Manager/Provider.
  /// </summary>
  ISourceCache<DeviceVisualizationConfig, string> Cache { get; }

  /// <summary>
  /// Retrieves a device visualization config from storage by its unique identifier.
  /// </summary>
  /// <param name="id">The unique ID of the config.</param>
  /// <returns>The config instance if found; otherwise, null.</returns>
  DeviceVisualizationConfig? GetConfig(string id);

  /// <summary>
  /// Retrieves all device visualization configs from storage that match the provided device id.
  /// </summary>
  /// <param name="deviceId"></param>
  /// <returns>An enumerable of configs, empty if none are found.</returns>
  IEnumerable<DeviceVisualizationConfig> GetConfigsByDeviceId(string deviceId);

  /// <summary>
  /// Retrieves all currently available configs.
  /// </summary>
  IReadOnlyCollection<DeviceVisualizationConfig> GetAll();

  /// <summary>
  /// Adds a new config or updates an existing one in the storage.
  /// </summary>
  void AddOrUpdate(DeviceVisualizationConfig device);

  /// <summary>
  /// Removes a config from the storage by its unique ID.
  /// </summary>
  void Remove(string id);

  /// <summary>
  /// Purges all configs from the storage.
  /// </summary>
  void Clear();
}
