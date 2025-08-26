using Ares.Datamodel.Device;

namespace Ares.Core.Device.Remote;
/// <summary>
/// This is responsible for the loading and management of the devices during the lifetime
/// of the application. 
/// * stores/loads devices from the database
/// * populates the device repository
/// </summary>
public interface IRemoteDeviceManager
{
  Task LoadDevices();

  Task<RemoteDevice> CreateDevice(string name, string url);

  Task<bool> RemoveDevice(string deviceId);

  Task UpdateDevice(RemoteDeviceConfig config);

  Task UpdateDeviceSettings(DeviceSettings deviceSettings);
}
