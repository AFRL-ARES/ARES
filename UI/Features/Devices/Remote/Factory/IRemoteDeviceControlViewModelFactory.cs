using Ares.Toolkit.Device.UI;
using UI.Application.Devices;

namespace UI.Features.Devices.Remote.Factory;

public interface IRemoteDeviceControlViewModelFactory
{
  IDeviceUnitControlViewModel Create(IAresDeviceAdapter adapter);
}
