using UI.Application.Devices.Repos;
using UI.Features.Devices.Shared;
using Ares.Toolkit.Device.UI;
using Ares.Device;

namespace UI.Features.Devices.Remote;

public class RemoteDeviceControlViewModelFactory : IDeviceViewModelFactory
{
  private readonly IDeviceAdapterRepository _deviceAdapterRepo;

  public RemoteDeviceControlViewModelFactory(IDeviceAdapterRepository deviceAdapterRepo) 
  {
    _deviceAdapterRepo = deviceAdapterRepo;
  }

  public IDeviceUnitControlViewModel Create(IAresDevice device)
  {
    var adapter = _deviceAdapterRepo.Items.FirstOrDefault(r => r.Id == device.UniqueId);

    if(adapter is not null)
      return (IDeviceUnitControlViewModel)new RemoteDeviceUnitViewModel(adapter);

    throw new NotImplementedException();
  }
}

