using Ares.Core.Device.Sila;
using Ares.Toolkit.Device.UI;
using UI.Application.Devices;

namespace UI.Features.Devices.Sila.Factory;

public interface ISilaDeviceControlViewModelFactory
{
  IDeviceUnitControlViewModel Create(SilaDevice silaDevice);
}
