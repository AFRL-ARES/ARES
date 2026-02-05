using Ares.Device;

namespace Ares.Core.Device.Repos;

public interface IDeviceDriverRepo
{
  void Register(DeviceDriver driver);
  DeviceDriver? GetByName(string name);
  IEnumerable<DeviceDriver> GetAll();
}