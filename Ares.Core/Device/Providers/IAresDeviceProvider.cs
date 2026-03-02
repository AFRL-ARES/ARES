using Ares.Device;
using DynamicData;

namespace Ares.Core.Device.Providers;

/// <summary>
/// A functional interface for components that need to observe device state.
/// Typically injected into UI or monitoring services.
/// </summary>
public interface IAresDeviceProvider : IEnumerable<IAresDevice>
{
  /// <summary>
  /// Provides a reactive stream of changes. 
  /// Subscribers automatically receive the current state followed by updates.
  /// </summary>
  IObservable<IChangeSet<IAresDevice, string>> Connect();

  /// <summary>
  /// Retrieves a device by its unique ID in a read-only context.
  /// </summary>
  IAresDevice? GetDevice(string id);

  /// <summary>
  /// Retrieves a device of a specific type by its unique ID.
  /// </summary>
  T? GetDevice<T>(string id) where T : class, IAresDevice;

  /// <summary>
  /// Retrieves a read-only snapshot of all currently available devices.
  /// </summary>
  IReadOnlyCollection<IAresDevice> GetAllDevices();

  /// <summary>
  /// Retrieves a read-only snapshot of all devices of a specific type.
  /// </summary>
  IReadOnlyCollection<T> GetAllDevices<T>() where T : IAresDevice;

  /// <summary>
  /// The total number of active devices currently in the repository.
  /// </summary>
  int Count { get; }
}
