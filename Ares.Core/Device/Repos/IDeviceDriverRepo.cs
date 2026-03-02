using Ares.Device;
using DynamicData;

namespace Ares.Core.Device.Repos;

public interface IDeviceDriverRepo : IDisposable, IEnumerable<DeviceDriver>
{
  /// <summary>
  /// Storage Engine accessors used by Manager/Provider.
  /// </summary>
  ISourceCache<DeviceDriver, string> Cache { get; }

  /// <summary>
  /// Retrieves a device driver from storage by its unique identifier.
  /// </summary>
  /// <param name="id">The unique ID of the driver.</param>
  /// <returns>The driver instance if found; otherwise, null.</returns>
  DeviceDriver? GetDriverById(string id);

  /// <summary>
  /// Retrieves a device driver from storage by its name.
  /// </summary>
  /// <param name="name"></param>
  /// <returns>The driver instance if found, otherwise, null.</returns>
  DeviceDriver? GetDriverByName(string name);

  /// <summary>
  /// Retrieves all currently available devices.
  /// </summary>
  IReadOnlyCollection<DeviceDriver> GetAllDrivers();

  /// <summary>
  /// Adds a new device or updates an existing one in the storage.
  /// </summary>
  void AddOrUpdate(DeviceDriver device);

  /// <summary>
  /// Removes a device from the storage by its unique ID.
  /// </summary>
  void Remove(string id);

  /// <summary>
  /// Purges all devices from the storage.
  /// </summary>
  void Clear();
}