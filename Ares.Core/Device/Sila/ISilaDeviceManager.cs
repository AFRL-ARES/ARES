using Tecan.Sila2;

namespace Ares.Core.Device.Sila;

public interface ISilaDeviceManager
{
  Task<bool> RemoveDevice(string deviceId);
  Task<SilaDevice?> Create(ServerData data);
  Task<IEnumerable<ServerData>> UpdateAvailableSilaDevices();
  Task<SilaDevice?> Create(string address, int port);
  Task LoadSilaDevices();
}
