using Ares.Services.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Google.Protobuf.WellKnownTypes;

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
    var connectRequest = new ConnectRequest { DeviceId = SelectedDeviceId, PortName = SelectedSerialPort };
    var connectionResponse = _syringePumpClient.Connect(connectRequest);
  }

  protected override SyringePumpUnitControlViewModel CreateUnitVm(AresDeviceDescription description)
  {
    var unitVm = new SyringePumpUnitControlViewModel(description.Id, description.Name, _syringePumpClient);
    return unitVm;
  }

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var sDevInfoResponse = await _syringePumpClient.GetAllSyringePumpsAsync(new Empty());
    var sNames = sDevInfoResponse.SyringePumps.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return sNames;
  }
}
