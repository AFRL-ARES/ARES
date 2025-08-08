using Ares.SyringePump.Ne1000.Messaging;
using Google.Protobuf.WellKnownTypes;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.SyringePump;

public class SyringePumpWorkspaceControlViewModel : SerialDeviceConnectorViewModel<SyringePumpUnitControlViewModel>
{
  private readonly SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;

  public SyringePumpWorkspaceControlViewModel(AresDevices.AresDevicesClient devicesClient,
    SyringePumpRpc.SyringePumpRpcClient syringePumpClient) : base(devicesClient)
  {
    _syringePumpClient = syringePumpClient;
  }

  public void Connect()
  {
    var connectRequest = new ConnectRequest { DeviceName = SelectedDeviceName, PortName = SelectedSerialPort };
    var connectionResponse = _syringePumpClient.Connect(connectRequest);
  }

  protected override SyringePumpUnitControlViewModel CreateUnitVm(string deviceName)
  {
    var unitVm = new SyringePumpUnitControlViewModel(deviceName, _syringePumpClient);
    return unitVm;
  }

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var sDevInfoResponse = await _syringePumpClient.GetAllSyringePumpsAsync(new Empty());
    var sNames = sDevInfoResponse.SyringePumps.Select(sDevInfo => sDevInfo.Name);
    return sNames;
  }
}
