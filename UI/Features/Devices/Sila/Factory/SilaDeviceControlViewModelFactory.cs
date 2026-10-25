using Ares.Toolkit.Device.UI;
using UI.Infrastructure.Devices.Sila;

namespace UI.Features.Devices.Sila.Factory;

public class SilaDeviceControlViewModelFactory : ISilaDeviceControlViewModelFactory
{
  public SilaDeviceControlViewModelFactory()
  {
    
  }

  public IDeviceUnitControlViewModel Create(SilaDeviceAdapter silaDevice)
    => new SilaDeviceUnitViewModel(silaDevice);
}
