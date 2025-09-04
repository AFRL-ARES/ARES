using System.Reactive.Linq;
using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.Devices;

public class DeviceAdapterManager(
    AresDevices.AresDevicesClient _devicesClient,
    DeviceAdapterRepository _deviceAdapterRepository,
    ILogger<DeviceAdapterManager> _logger) : IAsyncDisposable
{
  private readonly CancellationTokenSource _cts = new();
  private IDisposable? _subscription;
  private bool _isErrorState;

  public void Activate()
  {
    _subscription = Observable.Interval(TimeSpan.FromSeconds(5))
        .StartWith(0) // run immediately
        .SelectMany(async _ =>
        {
          try
          {
            // let's stick to remote devices for now as the built-int devices have their
            // own logic in viewmodels
            var devices = await _devicesClient.ListRemoteAresDevicesAsync(
                new Empty(),
                cancellationToken: _cts.Token);

            if(_isErrorState)
            {
              _logger.LogInformation("Device polling recovered.");
              _isErrorState = false;
            }

            return devices;
          }
          catch(OperationCanceledException)
          {
            return null; // expected shutdown
          }
          catch(Exception ex)
          {
            if(!_isErrorState)
            {
              _logger.LogError(ex, "Error polling remote Ares devices.");
              _isErrorState = true;
            }
            return null;
          }
        })
        .Where(devices => devices is not null)
        .Subscribe(devices =>
        {
          try
          {
            var remoteIds = devices!.Devices.Select(d => d.UniqueId).ToHashSet();
            var existingIds = _deviceAdapterRepository.Keys.ToHashSet();

            var newDevices = remoteIds.Except(existingIds);
            var removedDevices = existingIds.Except(remoteIds);

            var newAdapters = newDevices
                    .Select(id => new RemoteDeviceAdapter(_devicesClient, id)).ToArray();

            var removedAdapters = _deviceAdapterRepository.Items.Where(da => removedDevices.Contains(da.Id)).OfType<IAsyncDisposable>().ToArray();
            foreach(var adapter in removedAdapters)
            {
              _ = adapter.DisposeAsync();
            }

            foreach(var adapter in newAdapters)
            {
              _ = adapter.Activate();
            }

            _deviceAdapterRepository.Edit(updater =>
            {
              updater.AddOrUpdate(newAdapters); // add/update
              updater.RemoveKeys(removedDevices);   // remove stale
            });
          }
          catch(Exception ex)
          {
            _logger.LogError(ex, "Error updating device adapter repository.");
          }
        });
  }

  public ValueTask DisposeAsync()
  {
    _cts.Cancel();
    _subscription?.Dispose();
    _cts.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
