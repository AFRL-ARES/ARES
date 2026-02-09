using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using Tc0304.Services;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Tc0304;

namespace UI.Features.Devices.Tc0304;

public class Tc0304DeviceControlViewModelFactory : DeviceConnectorViewModelFactory<Tc0304UnitControlViewModel>
{
  private readonly TC0304Rpc.TC0304RpcClient _client;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public Tc0304DeviceControlViewModelFactory(TC0304Rpc.TC0304RpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _client = client;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new Tc0304UnitControlViewModel(deviceId, deviceName, _client));

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var loggerInfo = await _client.GetAllTc0304sAsync(new Empty());
    var infos = loggerInfo.Devices.Select(i => new AresDeviceDescription(i.Id, i.Name)).ToArray();
    return infos;
  }
}
