using Ares.Device;

namespace Ares.Core.Device;

public interface IDeviceCommandInterpreterRepo : ICollection<IDeviceCommandInterpreter<IAresDevice>>
{
  bool Remove(string deviceId);

  TDevice[] GetAresDevices<TDevice>() where TDevice : IAresDevice;
  IAresDevice[] GetAresDevices();
  IAresDevice? GetAresDevice(string deviceId);
  TDevice? GetAresDevice<TDevice>(string deviceId) where TDevice : IAresDevice;
  IDeviceCommandInterpreter<IAresDevice> GetCommandInterpreterByDeviceId(string deviceId);
  IDeviceCommandInterpreter<IAresDevice>[] GetSnapshot();
}
