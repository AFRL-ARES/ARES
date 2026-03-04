using Ares.Datamodel.Device;
using Ares.Device;
using DynamicData;

namespace Ares.Core.Device.Providers;

/// <summary>
/// A functional interface for components that need to observe device state.
/// Typically injected into UI or monitoring services.
/// </summary>
public interface IDeviceConfigProvider : IEnumerable<DeviceConfig>
{
  /// <summary>
  /// Provides a reactive stream of changes. 
  /// Subscribers automatically receive the current state followed by updates.
  /// </summary>
  IObservable<IChangeSet<DeviceConfig, string>> Connect();

  /// <summary>
  /// Retrieves a device config by its unique ID in a read-only context.
  /// </summary>
  DeviceConfig? GetConfig(string id);

  /// <summary>
  /// Retrieves a read-only snapshot of all currently available device configs.
  /// </summary>
  IReadOnlyCollection<DeviceConfig> GetAllConfigs();

  /// <summary>
  /// The total number of device configs currently in the repository.
  /// </summary>
  int Count { get; }
}
