using DynamicData;
using UI.Features.Devices.Shared;

namespace UI.Infrastructure.Repos
{
  public interface IDeviceControlViewModelRepo : ISourceList<DeviceUnitControlViewModel>
  {
    void Initialize();
  }
}
