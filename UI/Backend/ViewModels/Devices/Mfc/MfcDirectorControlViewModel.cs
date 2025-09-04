using Ares.Alicat.Mfc.Messaging;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.Devices.Mfc;

public class MfcDirectorControlViewModel : SerialDeviceConnectorViewModel<MfcUnitControlViewModel>
{
  private readonly MfcRpc.MfcRpcClient _mfcClient;

  public MfcDirectorControlViewModel(AresDevices.AresDevicesClient devicesClient, MfcRpc.MfcRpcClient mfcClient) : base(devicesClient)
  {
    _mfcClient = mfcClient;
  }

  protected override MfcUnitControlViewModel CreateUnitVm(AresDeviceDescription description)
  {
    var vm = new MfcUnitControlViewModel(description.Id, description.Name, _mfcClient);
    return vm;
  }

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devInfos = await _mfcClient.GetAllMfcsAsync(new Empty());
    var devNames = devInfos.Mfcs.Select(devInfo => new AresDeviceDescription(devInfo.Id, devInfo.Name)).ToArray();
    return devNames;
  }

}
