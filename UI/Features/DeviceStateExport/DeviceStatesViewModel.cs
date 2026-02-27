using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.DeviceStateExport;

public partial class DeviceStatesViewModel : ReactiveObject
{
  readonly DevicesService _devicesClient;

  public DeviceStatesViewModel(DevicesService devicesClient)
  {
    _devicesClient = devicesClient;
    _ = _devicesClient
      .ListAresDevices(new Empty(), null)
      .ContinueWith(task => { if(task.IsCompletedSuccessfully) AvailableDevices = task.Result.AresDevices; });
  }
  public string? SelectedDeviceName { get; set; }

  public void ChooseDevice(string deviceName)
  {

  }

  [Reactive]
  public partial IEnumerable<DeviceInfo>? AvailableDevices { get; private set; }
}
