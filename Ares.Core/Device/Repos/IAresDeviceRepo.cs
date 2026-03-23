using Ares.Device;
using DynamicData;

namespace Ares.Core.Device.Repos;

/// <summary>
/// The low-level, thread-safe storage for devices.
/// This is the 'Single Source of Truth' for the ARES system.
/// </summary>
public interface IAresDeviceRepo : IDisposable, IEnumerable<IAresDevice>
{
  /// <summary>
  /// Storage Engine accessors used by Manager/Provider.
  /// </summary>
  ISourceCache<IAresDevice, string> Cache { get; }

  /// <summary>
  /// Retrieves a device from the storage by its unique identifier.
  /// </summary>
  /// <param name="id">The unique ID of the device.</param>
  /// <returns>The device instance if found; otherwise, null.</returns>
  IAresDevice? GetDevice(string id);

  /// <summary>
  /// Retrieves a device of a specific type by its unique identifier.
  /// </summary>
  T? GetDevice<T>(string id) where T : class, IAresDevice;

  /// <summary>
  /// Retrieves all currently available devices.
  /// </summary>
  IReadOnlyCollection<IAresDevice> GetAll();

  /// <summary>
  /// Retrieves all currently available devices of a specific type.
  /// </summary>
  IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice;

  /// <summary>
  /// Adds a new device or updates an existing one in the storage.
  /// </summary>
  void AddOrUpdate(IAresDevice device);

  /// <summary>
  /// Removes a device from the storage by its unique ID.
  /// </summary>
  void Remove(string id);

  /// <summary>
  /// Purges all devices from the storage.
  /// </summary>
  void Clear();
}
