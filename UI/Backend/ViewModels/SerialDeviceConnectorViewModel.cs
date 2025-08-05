using Ares.Messaging.Device;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace UI.Backend.ViewModels;

public abstract class SerialDeviceConnectorViewModel<TDeviceUnitVm> : ReactiveObject, IAsyncDisposable where TDeviceUnitVm : SerialDeviceUnitViewModel
{
  private readonly ISourceCache<TDeviceUnitVm, string> _connectedSerialDeviceUnitControlVmsSource =
  new SourceCache<TDeviceUnitVm, string>(vm => vm.DeviceName);
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public SerialDeviceConnectorViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;

    _connectedSerialDeviceUnitControlVmsSource.Connect().Bind(out var connectedSerialDeviceUnitControlVms).Subscribe();
    ConnectedSerialDeviceUnitControlVms = connectedSerialDeviceUnitControlVms;
  }

  protected abstract Task<IEnumerable<string>> GetDeviceNames();

  public void Start(TimeSpan interval)
  {
    _deviceUpdater = Observable.Interval(interval).Prepend(0).Subscribe(_ => UpdateAvailableDevices());
  }

  private async Task UpdateAvailableDevices()
  {
    var deviceNames = await GetDeviceNames();
    foreach (var deviceName in deviceNames)
    {
      var deviceStatusRequest = new DeviceStatusRequest { DeviceName = deviceName };
      var deviceStatusResponse = _devicesClient.GetDeviceStatus(deviceStatusRequest);

      if (deviceStatusResponse.DeviceState == DeviceState.Active)
      {
        if (ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          continue;

        var unitVm = CreateUnitVm(deviceName);
        _connectedSerialDeviceUnitControlVmsSource.AddOrUpdate(unitVm);
        continue;
      }

      if (deviceStatusResponse.DeviceState == DeviceState.Inactive)
      {
        if (ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          _connectedSerialDeviceUnitControlVmsSource.Remove(deviceName);

        continue;
      }

      if (deviceStatusResponse.DeviceState == DeviceState.Error)
        if (ConnectedSerialDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          _connectedSerialDeviceUnitControlVmsSource.Remove(deviceName);
    }
  }

  protected abstract TDeviceUnitVm CreateUnitVm(string deviceName);

  public async ValueTask DisposeAsync()
  {
    _deviceUpdater.Dispose();
    var vms = ConnectedSerialDeviceUnitControlVms.OfType<IAsyncDisposable>();
    foreach (var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }

  public string[]? DiscoveredSerialPorts { get; set; }

  public string? SelectedSerialPort { get; set; }

  public string[]? DiscoveredDeviceNames { get; set; }

  public string? SelectedDeviceName { get; set; }
  public ReadOnlyObservableCollection<TDeviceUnitVm> ConnectedSerialDeviceUnitControlVms { get; }

}