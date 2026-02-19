namespace Ares.Core.Device.Repos;

public interface IDeviceDriverRepo
{
  void Register(DeviceDriver driver);
  DeviceDriver? GetByName(string name);
  DeviceDriver? GetById(string id);
  IEnumerable<DeviceDriver> GetAll();
}