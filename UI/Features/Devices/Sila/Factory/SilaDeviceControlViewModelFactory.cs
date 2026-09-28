using Ares.Core.Device.Sila;
using Ares.Toolkit.Device.UI;

namespace UI.Features.Devices.Sila.Factory;

public class SilaDeviceControlViewModelFactory : ISilaDeviceControlViewModelFactory
{
  public SilaDeviceControlViewModelFactory()
  {
    
  }

  public IDeviceUnitControlViewModel Create(SilaDevice silaDevice)
    => new SilaDeviceUnitViewModel(silaDevice);
}
