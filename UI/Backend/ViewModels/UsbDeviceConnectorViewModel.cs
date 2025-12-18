using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using DynamicData;
using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class UsbDeviceConnectorViewModel<TDeviceUnitVm> : ReactiveObject, IAsyncDisposable where TDeviceUnitVm : DeviceUnitControlViewModel
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

  protected abstract Task<AresDeviceDescription[]> GetDeviceDescriptions();

  public void Start(TimeSpan interval)
  {
    _deviceUpdater = Observable.Interval(interval).Prepend(0).Subscribe(_ => UpdateAvailableDevices());
  }

  private async Task UpdateAvailableDevices()
  {
    var devices = await GetDeviceDescriptions();

    foreach(var deviceDesc in devices)
    {

      var deviceStatusRequest = new DeviceStatusRequest { DeviceId = deviceDesc.Id };
      var deviceOperationalStatusResponse = _devicesClient.GetDeviceStatus(deviceStatusRequest);

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Active)
      {
        if(ConnectedUsbDeviceUnitControlVms.Any(vm => vm.DeviceId == deviceDesc.Id))
          continue;

        var unitVm = CreateUnitVm(deviceDesc.Id, deviceDesc.Name);
        _connectedUsbDeviceUnitControlVmsSource.AddOrUpdate(unitVm);
        continue;
      }

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Inactive)
      {
        if(ConnectedUsbDeviceUnitControlVms.FirstOrDefault(vm => vm.DeviceId == deviceDesc.Id) is TDeviceUnitVm vm)
          _connectedUsbDeviceUnitControlVmsSource.Remove(vm);

        continue;
      }

      if(deviceOperationalStatusResponse.OperationalState == OperationalState.Error)
        if(ConnectedUsbDeviceUnitControlVms.FirstOrDefault(vm => vm.DeviceId == deviceDesc.Id) is TDeviceUnitVm vm)
          _connectedUsbDeviceUnitControlVmsSource.Remove(vm);
    }
  }

  protected abstract TDeviceUnitVm CreateUnitVm(string deviceId, string deviceName);

  public async ValueTask DisposeAsync()
  {
    _deviceUpdater.Dispose();
    var vms = ConnectedUsbDeviceUnitControlVms.OfType<IAsyncDisposable>();
    foreach(var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }

  public string[]? DiscoveredDeviceNames { get; set; }

  public string? SelectedDeviceName { get; set; }

  public ReadOnlyObservableCollection<TDeviceUnitVm> ConnectedUsbDeviceUnitControlVms { get; }
}
