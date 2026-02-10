using DynamicData;

namespace UI.Application.Devices.Repos
{
  public interface IDeviceControlViewModelRepo : ISourceList<DeviceUnitControlViewModel>
  {
    void Initialize();
  }
}
