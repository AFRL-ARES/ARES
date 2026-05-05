using Ares.Datamodel.Visualizing.Local;
using DynamicData;

namespace Ares.Core.Visualization.Providers;

/// <summary>
/// A functional interface for components that need to observe device state.
/// Typically injected into UI or monitoring services.
/// </summary>
public interface IDeviceVisualizationConfigProvider : IEnumerable<DeviceVisualizationConfig>
{
  /// <summary>
  /// Provides a reactive stream of changes. 
  /// Subscribers automatically receive the current state followed by updates.
  /// </summary>
  IObservable<IChangeSet<DeviceVisualizationConfig, string>> Connect();

  /// <summary>
  /// Retrieves a device visualization config by its unique ID in a read-only context.
  /// </summary>
  DeviceVisualizationConfig? GetConfig(string id);

  /// <summary>
  /// Retreives a device visualization config based on the corresponding device id.
  /// </summary>
  /// <param name="deviceId"></param>
  /// <returns></returns>
  IEnumerable<DeviceVisualizationConfig> GetConfigsByDeviceId(string deviceId);

  /// <summary>
  /// Retrieves a read-only snapshot of all currently available device visualization configs.
  /// </summary>
  IReadOnlyCollection<DeviceVisualizationConfig> GetAllConfigs();

  /// <summary>
  /// The total number of device visualization configs currently in the repository.
  /// </summary>
  int Count { get; }
}

