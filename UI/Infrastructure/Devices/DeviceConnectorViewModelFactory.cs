using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UI.Application.Devices;
using UI.Application.Devices.Repos;
using Ares.Core.Device.Repos;
using Ares.Core.Device.Providers;
using Ares.Device;

namespace UI.Infrastructure.Devices;

public class DeviceConnectorViewModelFactory : ReactiveObject, IAsyncDisposable
{
  private readonly IAresDeviceProvider _deviceProvider;
  protected readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private IDisposable _deviceUpdater = Disposable.Empty;

  public DeviceConnectorViewModelFactory(IAresDeviceProvider deviceProvider, IDeviceControlViewModelRepo deviceControlViewModelRepo)
  {
    _deviceProvider = deviceProvider;
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
    var availableDevices = _deviceProvider.GetAllDevices();
    foreach(var device in availableDevices)
    {
      if(device is not null && device.Status.OperationalState == OperationalState.Active)
      {
        if(_deviceControlViewModelRepo.Items.Any(vm => vm.DeviceId.Equals(device.UniqueId)))
          continue;

        CreateAndAddViewModel(device.UniqueId, device.Name);
        continue;
      }
    }
  }

  protected void CreateAndAddViewModel(string deviceId, string deviceName)
  {
    throw new NotImplementedException();
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
