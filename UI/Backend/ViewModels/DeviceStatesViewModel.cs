using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels;

public class DeviceStatesViewModel : ReactiveObject
{
  readonly AresDevices.AresDevicesClient _devicesClient;

  public DeviceStatesViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
    _devicesClient
      .ListAresDevicesAsync(new Empty()).ResponseAsync
      .ContinueWith(task => AvailableDevices = task.Result.AresDevices);
  }
  public string? SelectedDeviceName { get; set; }

  public void ChooseDevice(string deviceName)
  {

  }

  [Reactive]
  public IEnumerable<AresDeviceInfo>? AvailableDevices { get; private set; }
}
