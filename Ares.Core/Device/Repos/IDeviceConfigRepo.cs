using Ares.Datamodel.Device;
using DynamicData;

namespace Ares.Core.Device.Repos;

public interface IDeviceConfigRepo : IDisposable, IEnumerable<DeviceConfig>
{
  /// <summary>
  /// Storage Engine accessors used by Manager/Provider.
  /// </summary>
  ISourceCache<DeviceConfig, string> Cache { get; }

  /// <summary>
  /// Retrieves a device config from storage by its unique identifier.
  /// </summary>
  /// <param name="id">The unique ID of the config.</param>
  /// <returns>The config instance if found; otherwise, null.</returns>
  DeviceConfig? GetConfig(string id);

  /// <summary>
  /// Retrieves all currently available configs.
  /// </summary>
  IReadOnlyCollection<DeviceConfig> GetAll();

  /// <summary>
  /// Adds a new config or updates an existing one in the storage.
  /// </summary>
  void AddOrUpdate(DeviceConfig device);

  /// <summary>
  /// Removes a config from the storage by its unique ID.
  /// </summary>
  void Remove(string id);

  /// <summary>
  /// Purges all configs from the storage.
  /// </summary>
  void Clear();
}
