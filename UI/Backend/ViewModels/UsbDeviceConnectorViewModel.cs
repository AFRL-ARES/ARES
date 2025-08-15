using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Ares.Datamodel.Device;
using Ares.Services.Device;

namespace UI.Backend.ViewModels;

public abstract class UsbDeviceConnectorViewModel<TDeviceUnitVm> : ReactiveObject, IAsyncDisposable where TDeviceUnitVm : UsbDeviceUnitViewModel
{
  private readonly ISourceCache<TDeviceUnitVm, string> _connectedUsbDeviceUnitControlVmsSource = new SourceCache<TDeviceUnitVm, string>(vm => vm.DeviceName);
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public UsbDeviceConnectorViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;

    _connectedUsbDeviceUnitControlVmsSource.Connect().Bind(out var connectedUsbDeviceUnitControlVms).Subscribe();
    ConnectedUsbDeviceUnitControlVms = connectedUsbDeviceUnitControlVms;
  }

  protected abstract Task<IEnumerable<string>> GetDeviceNames();

  protected abstract Task<IEnumerable<string>> GetDeviceIds();

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
      var deviceOperationalStatusResponse = _devicesClient.GetDeviceStatus(deviceStatusRequest);

      if (deviceOperationalStatusResponse.OperationalState == OperationalState.Active)
      {
        if (ConnectedUsbDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          continue;

        var unitVm = CreateUnitVm(deviceName);
        _connectedUsbDeviceUnitControlVmsSource.AddOrUpdate(unitVm);
        continue;
      }

      if (deviceOperationalStatusResponse.OperationalState == OperationalState.Inactive)
      {
        if (ConnectedUsbDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          _connectedUsbDeviceUnitControlVmsSource.Remove(deviceName);

        continue;
      }

      if (deviceOperationalStatusResponse.OperationalState == OperationalState.Error)
        if (ConnectedUsbDeviceUnitControlVms.Any(vm => vm.DeviceName.Equals(deviceName)))
          _connectedUsbDeviceUnitControlVmsSource.Remove(deviceName);
    }
  }

  protected abstract TDeviceUnitVm CreateUnitVm(string deviceName);

  public async ValueTask DisposeAsync()
  {
    _deviceUpdater.Dispose();
    var vms = ConnectedUsbDeviceUnitControlVms.OfType<IAsyncDisposable>();
    foreach (var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }

  public string[]? DiscoveredDeviceNames { get; set; }

  public string? SelectedDeviceName { get; set; }

  public ReadOnlyObservableCollection<TDeviceUnitVm> ConnectedUsbDeviceUnitControlVms { get; }
}
