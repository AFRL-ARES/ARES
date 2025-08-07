using Ares.Alicat.Mfc.Messaging;
using Google.Protobuf.WellKnownTypes;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.Devices.Mfc;

public class MfcDirectorControlViewModel : SerialDeviceConnectorViewModel<MfcUnitControlViewModel>
{
    private readonly MfcRpc.MfcRpcClient _mfcClient;

    public MfcDirectorControlViewModel(AresDevices.AresDevicesClient devicesClient, MfcRpc.MfcRpcClient mfcClient) : base(devicesClient)
    {
        _mfcClient = mfcClient;
    }

    protected override MfcUnitControlViewModel CreateUnitVm(string deviceName)
    {
        var vm = new MfcUnitControlViewModel(deviceName, _mfcClient);
        return vm;
    }

    protected override async Task<IEnumerable<string>> GetDeviceNames()
    {
        var devInfos = await _mfcClient.GetAllMfcsAsync(new Empty());
        var devNames = devInfos.Mfcs.Select(devInfo => devInfo.Name);
        return devNames;
    }

}
