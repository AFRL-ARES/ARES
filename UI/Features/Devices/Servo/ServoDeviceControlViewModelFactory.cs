using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using HerkulexDRS.Services;
using UI.Application.Devices.Repos;
using UI.Infrastructure.Devices;
using UI.Application.Devices;

namespace UI.Features.Devices.Servo;

public class ServoDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<ServoUnitControlViewModel>
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public ServoDeviceControlViewModelFactory(HerkulexDRSRpc.HerkulexDRSRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _client = client;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName) 
    => _deviceControlViewModelRepo.Add(new ServoUnitControlViewModel(deviceId, deviceName, _client));

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var servoInfo = await _client.GetAllServosAsync(new Empty());
    var servos = servoInfo.Devices.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return servos;
  }
}

