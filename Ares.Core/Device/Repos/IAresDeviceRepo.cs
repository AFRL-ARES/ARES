using Ares.Device;

namespace Ares.Core.Device.Repos;

public interface IAresDeviceRepo : ICollection<IAresDevice>
{
  bool Remove(string deviceId);

  TDevice[] GetAresDevices<TDevice>() where TDevice : IAresDevice;
  IAresDevice? GetAresDevice(string deviceId);
  TDevice? GetAresDevice<TDevice>(string deviceId) where TDevice : IAresDevice;
  IAresDevice[] GetSnapshot();
}
