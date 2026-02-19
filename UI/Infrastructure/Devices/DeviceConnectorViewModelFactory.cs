using Ares.Datamodel.Device;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UI.Application.Devices;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices;

public class DeviceConnectorViewModelFactory : ReactiveObject, IAsyncDisposable
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public DeviceConnectorViewModelFactory(AresDevices.AresDevicesClient devicesClient, IDeviceControlViewModelRepo deviceControlViewModelRepo)
  {
    _devicesClient = devicesClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

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
      var deviceStatusResponse = _devicesClient.GetDeviceStatus(deviceStatusRequest);

      if(deviceStatusResponse.OperationalState == OperationalState.Active)
      {
        if(_deviceControlViewModelRepo.Items.Any(vm => vm.DeviceId.Equals(description.DeviceId)))
          continue;

        CreateAndAddViewModel(description.DeviceId, description.DeviceName);
        continue;
      }
    }
  }

  private async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var response = await _devicesClient.GetAllAvailableDevicesAsync(new Empty());
    return response.Devices;
  }

  protected void CreateAndAddViewModel(string deviceId, string deviceName)
  {

  }

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
