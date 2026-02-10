using Ares.Services.Device;
using ChemyxPumpPlugin.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using UI.Application.Devices.Repos;
using UI.Infrastructure.Devices;
using UI.Application.Devices;

namespace UI.Features.Devices.ChemyxPump;

public class ChemyxPumpControlViewModelFactory : DeviceConnectorViewModelFactory<ChemyxPumpUnitControlViewModel>
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _pumpClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public ChemyxPumpControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    ChemyxPumpRpc.ChemyxPumpRpcClient pumpClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _pumpClient = pumpClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new ChemyxPumpUnitControlViewModel(deviceId, deviceName, _pumpClient));

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _pumpClient.GetAllPumpsAsync(new Empty());
    var pumps = devInfos.Devices.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return pumps;
  }
}

