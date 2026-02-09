using Ares.Alicat.Mfc.Messaging;
using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Devices.Mfc;

namespace UI.Features.Devices.Mfc;

public class MFCDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<MfcUnitControlViewModel>
{
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public MFCDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    MfcRpc.MfcRpcClient mfcClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _mfcClient = mfcClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
   => _deviceControlViewModelRepo.Add(new MfcUnitControlViewModel(deviceId, deviceName, _mfcClient));


  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _mfcClient.GetAllMfcsAsync(new Empty());
    var mfcs = devInfos.Mfcs.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return mfcs;
  }
}
