using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ValveController.Services;
using UI.Application.Devices.Repos;
using UI.Infrastructure.Devices;
using UI.Application.Devices;

namespace UI.Features.Devices.ValveController;

public class ValveControllerDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<ValveControllerUnitControlViewModel>
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public ValveControllerDeviceControlViewModelFactory(ValveControllerRpc.ValveControllerRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _client = client;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new ValveControllerUnitControlViewModel(deviceId, deviceName, _client));


  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var valveInfos = await _client.GetAllValveControllersAsync(new Empty());
    var valves = valveInfos.Devices.Select(v => new AresDeviceDescription(v.Id, v.Name)).ToArray();
    return valves;
  }
}
