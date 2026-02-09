using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Devices.Remote;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.Remote;

public class RemoteDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<RemoteDeviceUnitViewModel>
{
  private readonly DeviceAdapterRepository _deviceAdapterRepo;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public RemoteDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    DeviceAdapterRepository deviceAdapterRepo,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _deviceAdapterRepo = deviceAdapterRepo;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _devicesClient = devicesClient;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
  {
    var adapter = _deviceAdapterRepo.Items.FirstOrDefault(r => r.Id == deviceId);

    if(adapter is not null)
      _deviceControlViewModelRepo.Add(new RemoteDeviceUnitViewModel(adapter));
  }
    

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _devicesClient.GetAllRemoteDevicesConfigsAsync(new Empty());
    var remotedDevices = devInfos.Configs.Select(dev => new AresDeviceDescription(dev.UniqueId, dev.Name)).ToArray();
    return remotedDevices;
  }
}
