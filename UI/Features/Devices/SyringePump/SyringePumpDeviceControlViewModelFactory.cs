using Ares.Services.Device;
using Ares.SyringePump.Ne1000.Messaging;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.SyringePump;

namespace UI.Features.Devices.SyringePump;

public class SyringePumpDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<SyringePumpUnitControlViewModel>
{
  private readonly SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public SyringePumpDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    SyringePumpRpc.SyringePumpRpcClient syringePumpClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _syringePumpClient = syringePumpClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new SyringePumpUnitControlViewModel(deviceId, deviceName, _syringePumpClient));

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var sDevInfoResponse = await _syringePumpClient.GetAllSyringePumpsAsync(new Empty());
    var pumps = sDevInfoResponse.SyringePumps.Select(info => new AresDeviceDescription(info.Id, info.Name)).ToArray();
    return pumps;
  }
}
