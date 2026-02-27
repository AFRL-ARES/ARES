using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using UI.Infrastructure.Devices;
using UI.Application.Devices.Repos;
using Ares.Datamodel.Device;

namespace UI.Features.Devices.Remote;

public class RemoteDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<RemoteDeviceUnitViewModel>
{
  private readonly IDeviceAdapterRepository _deviceAdapterRepo;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DevicesService _devicesClient;

  public RemoteDeviceControlViewModelFactory(DevicesService devicesClient,
    IDeviceAdapterRepository deviceAdapterRepo,
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
    var devInfos = await _devicesClient.GetAllRemoteDevicesConfigs(new Empty(), null);
    var remotedDevices = devInfos.Configs.Select(dev => new AresDeviceDescription() { DeviceId = dev.UniqueId, DeviceName = dev.Name }).ToArray();
    return remotedDevices;
  }
}

