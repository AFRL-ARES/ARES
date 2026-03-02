using Ares.Datamodel.Device;
using Ares.Device;

namespace Ares.Core.Device.Managers;

public interface IDeviceManager
{
  Task<IAresDevice> Create(DeviceConfig config);
  Task<IAresDevice> Load(string deviceId, DeviceConfig config);
  Task<IAresDevice[]> Load(IEnumerable<DeviceConfig> configs);
  Task<IAresDevice> Update(string deviceId, DeviceConfig config);
  /// <summary>
  /// Retrieves all managed devices of a specific type.
  /// </summary>
  IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice;

  /// <summary>
  /// Retrieves a device of a specific type by its ID.
  /// </summary>
  T? GetDevice<T>(string id) where T : class, IAresDevice;
  Task Remove(string deviceId);
  Task LoadDevices();
}
