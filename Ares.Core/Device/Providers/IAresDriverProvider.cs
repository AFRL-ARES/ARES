using Ares.Core.Device.Plugins.Drivers;
using DynamicData;

namespace Ares.Core.Device.Providers;

public interface IAresDriverProvider : IEnumerable<DeviceDriver>
{
  /// <summary>
  /// Provides a reactive stream of changes. 
  /// Subscribers automatically receive the current state followed by updates.
  /// </summary>
  IObservable<IChangeSet<DeviceDriver, string>> Connect();

  /// <summary>
  /// Retrieves a device driver by its unique ID in a read-only context.
  /// </summary>
  DeviceDriver? GetDriverById(string id);

  /// <summary>
  /// Retrieves a device driver by its name in a read-only context.
  /// </summary>
  DeviceDriver? GetDriverByName(string name);

  /// <summary>
  /// Retrieves a read-only snapshot of all currently available devices.
  /// </summary>
  IReadOnlyCollection<DeviceDriver> GetAllDeviceDrivers();

  /// <summary>
  /// The total number of active devices currently in the repository.
  /// </summary>
  int Count { get; }
}
