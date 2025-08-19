using Ares.Core.Device.Remote;
using Ares.Datamodel.Device;

namespace Ares.Core.Analyzing;
internal class RemoteDeviceMonitor : IDisposable
{
  private readonly RemoteDevice _device;
  private readonly Task _monitorTask;
  private readonly CancellationTokenSource _tokenSource;
  private OperationalState _lastState = OperationalState.Unspecified;
  readonly IDeviceCache _deviceCache;

  public RemoteDeviceMonitor(RemoteDevice device, IDeviceCache deviceCache)
  {
    _deviceCache = deviceCache;
    _device = device;
    _tokenSource = new CancellationTokenSource();
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string DeviceId => _device.UniqueId;

  public void Dispose()
  {
    _tokenSource.Cancel();
    _monitorTask.ContinueWith(_ => _tokenSource.Dispose());
  }

  private Task<Task> Monitor(CancellationToken token)
  {
    return Task.Factory
      .StartNew(
        async (_) =>
        {
          while(!token.IsCancellationRequested)
          {
            await _device.UpdateOperationalStatus();

            if(_lastState != OperationalState.Active && _device.Status.OperationalState == OperationalState.Active)
            {
              await _device.UpdateInfo();
              await _device.UpdateSettings();
              await _device.UpdateCommands();
              await _deviceCache.CacheDeviceInfo(_device);
              await _deviceCache.CacheDeviceSettings(_device);
            }

            _lastState = _device.Status.OperationalState;

            await Task.Delay(TimeSpan.FromSeconds(5));
          }
        },
        token,
        TaskCreationOptions.LongRunning);
  }
}
