using Ares.Alicat.Mfc.Messaging;
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;

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
