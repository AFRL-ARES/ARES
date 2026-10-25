using Ares.Toolkit.Device.UI;
using UI.Infrastructure.Devices.Sila;

namespace UI.Features.Devices.Sila.Factory;

public interface ISilaDeviceControlViewModelFactory
{
  IDeviceUnitControlViewModel Create(SilaDeviceAdapter silaDevice);
}
