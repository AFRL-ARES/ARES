using Ares.Toolkit.Device.UI;
using DynamicData;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using UI.Application.Devices;
using UI.Application.Devices.Repos;

namespace UI.Features.Devices.Remote.Factory;

public class RemoteDeviceControlViewModelFactory : IRemoteDeviceControlViewModelFactory
{
  public RemoteDeviceControlViewModelFactory() { }

  public IDeviceUnitControlViewModel Create(IAresDeviceAdapter adapter)
    => new RemoteDeviceUnitViewModel(adapter);
}

