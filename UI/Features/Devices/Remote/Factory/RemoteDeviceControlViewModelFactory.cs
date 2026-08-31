using Ares.Toolkit.Device.UI;
using UI.Application.Devices;

namespace UI.Features.Devices.Remote.Factory;

public class RemoteDeviceControlViewModelFactory : IRemoteDeviceControlViewModelFactory
{
  public RemoteDeviceControlViewModelFactory() { }

  public IDeviceUnitControlViewModel Create(IAresDeviceAdapter adapter)
    => new RemoteDeviceUnitViewModel(adapter);
}

