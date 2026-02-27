using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UI.Application.Devices;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices;

public abstract class DeviceConnectorViewModelFactory(
    DevicesService devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo)
    : ReactiveObject, IAsyncDisposable
{
  protected readonly DevicesService _devicesClient = devicesClient;
  protected readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo = deviceControlViewModelRepo;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public void Start(TimeSpan interval)
  {
    _deviceUpdater = Observable.Defer(async () =>
    {
      try
      {
        await UpdateAvailableDevices();
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Error updating devices: {ex.Message}");
      }
      return Observable.Return(Unit.Default);
    })
    .Delay(interval)
    .Repeat()
    .Subscribe();
  }

  private async Task UpdateAvailableDevices()
  {
    var deviceDescriptions = await GetAvailableDevices();
    foreach(var description in deviceDescriptions)
    {
      var deviceStatusRequest = new DeviceStatusRequest { DeviceId = description.DeviceId };
      var deviceStatusResponse = await _devicesClient.GetDeviceStatus(deviceStatusRequest, null);

      if(deviceStatusResponse.OperationalState == OperationalState.Active)
      {
        if(_deviceControlViewModelRepo.Items.Any(vm => vm.DeviceId.Equals(description.DeviceId)))
          continue;

        CreateAndAddViewModel(description.DeviceId, description.DeviceName);
        continue;
      }
    }
  }

  protected abstract Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices();

  protected abstract void CreateAndAddViewModel(string deviceId, string deviceName);

  public async ValueTask DisposeAsync()
  {
    _deviceUpdater.Dispose();
    var vms = _deviceControlViewModelRepo.Items.OfType<IAsyncDisposable>();
    foreach(var vm in vms)
    {
      await vm.DisposeAsync();
    }
  }
}

public abstract class DeviceConnectorViewModelFactory<T>(
    DevicesService devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo)
    : DeviceConnectorViewModelFactory(devicesClient, deviceControlViewModelRepo)
    where T : DeviceUnitControlViewModel
{
}
