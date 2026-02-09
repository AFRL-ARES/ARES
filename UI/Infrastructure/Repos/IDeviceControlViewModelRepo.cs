using DynamicData;
using UI.Backend.ViewModels;

namespace UI.Infrastructure.Repos
{
  public interface IDeviceControlViewModelRepo : ISourceList<DeviceUnitControlViewModel>
  {
    void Initialize();
  }
}
