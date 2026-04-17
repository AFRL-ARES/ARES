using Tecan.Sila2;

namespace Ares.Core.Device.Sila;

public interface ISilaDeviceManager
{
  Task<SilaDevice?> Create(ServerData data);
  Task<IEnumerable<ServerData>> UpdateAvailableSilaDevices();
  Task<SilaDevice?> Create(string address, int port);
}
