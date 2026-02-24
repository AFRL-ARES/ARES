using Ares.Datamodel;
using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace UI.Backend.ViewModels.Components;

public partial class VisualizationSidebarViewModel : ReactiveObject
{
  //private readonly IVisualizationProvider _provider;
  private readonly IDisposable _devicesSubscription;

  public VisualizationSidebarViewModel()
  {
    //_provider = provider;
    //AvailableDevices = [];
    //_devicesSubscription = provider.AvailableDevicesStream
    //  .Subscribe(items =>
    //  {
    //    AvailableDevices = items;
    //  });
  }

  public async Task UpdateSelectedDeviceStateInformation()
  {
    //if(SelectedDevice is null)
    //  return;

    //var blah = await _provider.GetDeviceStateOptions(SelectedDevice.UniqueId);
    //AvailableDeviceStateItems = blah.Fields.Select(thing => thing.Key).ToArray();

    return;
  }

  [Reactive]
  public partial IEnumerable<DeviceInfo> AvailableDevices { get; set; }

  [Reactive]
  public partial DeviceInfo? SelectedDevice { get; set; }

  [Reactive]
  public partial AresStructSchema? SelectedDeviceStateSchema { get; set; }

  [Reactive]
  public partial string[]? AvailableDeviceStateItems { get; set; }
}
