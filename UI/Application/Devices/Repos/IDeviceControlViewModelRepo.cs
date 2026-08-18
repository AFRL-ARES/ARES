using Ares.Toolkit.Device.UI;
using DynamicData;

namespace UI.Application.Devices.Repos;

public interface IDeviceControlViewModelRepo : IObservableList<IDeviceUnitControlViewModel>, IDisposable
{
  void Initialize();
}
