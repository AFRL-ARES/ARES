using Ares.Core.Device.Providers;
using Ares.Core.Device.Remote;
using Ares.Core.Device.Sila;
using UI.Application.Devices.Repos;
using UI.Infrastructure.Devices.Remote;
using UI.Infrastructure.Devices.Sila;

namespace UI.Infrastructure.Devices;

public class DeviceAdapterManager : IAsyncDisposable
{
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IRemoteDeviceAdapterRepository _remoteDeviceAdapterRepository;
  private readonly ISilaDeviceAdapterRepository _silaDeviceAdapterRepository;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<DeviceAdapterManager> _logger;
  private readonly CancellationTokenSource _remoteDeviceCts = new();
  private readonly CancellationTokenSource _silaDeviceCts = new();
  private Task? _remoteDevicePollingTask;
  private Task? _silaDevicePollingTask;
  private bool _isErrorState;
  private IDictionary<string, RemoteDeviceAdapterMonitor> _remoteMonitors = new Dictionary<string, RemoteDeviceAdapterMonitor>();
  private IDictionary<string, SilaDeviceAdapterMonitor> _silaMonitors = new Dictionary<string, SilaDeviceAdapterMonitor>();

  public DeviceAdapterManager(IAresDeviceProvider deviceProvider,
    IRemoteDeviceAdapterRepository remoteAdapterRepo,
    ISilaDeviceAdapterRepository silaAdapterRepo,
    ILoggerFactory loggerFactory,
    ILogger<DeviceAdapterManager> logger)
  {
    _deviceProvider = deviceProvider;
    _remoteDeviceAdapterRepository = remoteAdapterRepo;
    _silaDeviceAdapterRepository = silaAdapterRepo;
    _loggerFactory = loggerFactory;
    _logger = logger;
  }

  public void Activate()
  {
    _remoteDevicePollingTask = Task.Run(() => PollRemoteDevicesAsync(_remoteDeviceCts.Token));
    _silaDevicePollingTask = Task.Run(() => PollSilaDevicesAsync(_silaDeviceCts.Token));
  }

  private async Task PollRemoteDevicesAsync(CancellationToken cancellationToken)
  {
    while(!cancellationToken.IsCancellationRequested)
    {
      try
      {
        var devices = _deviceProvider.GetAllDevices<RemoteDevice>();

        if(_isErrorState)
        {
          _logger.LogInformation("Device polling recovered.");
          _isErrorState = false;
        }

        await UpdateAdaptersFromDeviceList(devices);
      }
      catch(OperationCanceledException)
      {
        break;
      }
      catch(Exception ex)
      {
        if(!_isErrorState)
        {
          _logger.LogError(ex, "Error polling remote devices.");
          _isErrorState = true;
        }
      }

      try
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
      catch(OperationCanceledException)
      {
        break;
      }
    }
  }

  private async Task PollSilaDevicesAsync(CancellationToken cancellationToken)
  {
    while(!cancellationToken.IsCancellationRequested)
    {
      try
      {
        var devices = _deviceProvider.GetAllDevices<SilaDevice>();

        if(_isErrorState)
        {
          _logger.LogInformation("Device polling recovered.");
          _isErrorState = false;
        }

        await UpdateSilaAdaptersFromDeviceList(devices);
      }
      catch(OperationCanceledException)
      {
        break;
      }
      catch(Exception ex)
      {
        if(!_isErrorState)
        {
          _logger.LogError(ex, "Error polling SiLA devices.");
          _isErrorState = true;
        }
      }

      try
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
      catch(OperationCanceledException)
      {
        break;
      }
    }
  }

  private async Task UpdateAdaptersFromDeviceList(IReadOnlyCollection<RemoteDevice> devices)
  {
    try
    {
      var remoteIds = devices.Select(device => device.UniqueId).ToHashSet();
      var existingIds = _remoteDeviceAdapterRepository.Keys.ToHashSet();
      var newDevices = remoteIds.Except(existingIds);
      var removedDevices = existingIds.Except(remoteIds);

      var newAdapters = newDevices
        .Select(id => new RemoteDeviceAdapter(_deviceProvider, id, _loggerFactory.CreateLogger<RemoteDeviceAdapter>()))
        .ToArray();

      var removedAdapters = _remoteDeviceAdapterRepository.Items
        .Where(adapter => removedDevices.Contains(adapter.Id))
        .ToArray();

      foreach(var adapter in removedAdapters)
      {
        if(adapter is IAsyncDisposable asyncDisposable)
          await asyncDisposable.DisposeAsync();

        if(_remoteMonitors.Remove(adapter.Id, out var monitor))
          monitor.Dispose();
      }

      foreach(var adapter in newAdapters)
      {
        _ = adapter.Activate();
        var monitor = new RemoteDeviceAdapterMonitor(adapter, _loggerFactory.CreateLogger<RemoteDeviceAdapterMonitor>());
        _remoteMonitors[adapter.Id] = monitor;
        monitor.Activate();
      }

      _remoteDeviceAdapterRepository.Edit(updater =>
      {
        updater.AddOrUpdate(newAdapters);
        updater.RemoveKeys(removedDevices);
      });
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Error updating remote device adapters.");
    }
  }

  private async Task UpdateSilaAdaptersFromDeviceList(IReadOnlyCollection<SilaDevice> devices)
  {
    try
    {
      var silaDeviceIds = devices.Select(device => device.UniqueId).ToHashSet();
      var existingIds = _silaDeviceAdapterRepository.Keys.ToHashSet();
      var newDevices = silaDeviceIds.Except(existingIds);
      var removedDevices = existingIds.Except(silaDeviceIds);

      var newAdapters = newDevices
        .Select(id => new SilaDeviceAdapter(_deviceProvider, id, _loggerFactory.CreateLogger<SilaDeviceAdapter>()))
        .ToArray();

      var removedAdapters = _silaDeviceAdapterRepository.Items
        .Where(adapter => removedDevices.Contains(adapter.Id))
        .ToArray();

      foreach(var adapter in removedAdapters)
      {
        if(adapter is IAsyncDisposable asyncDisposable)
          await asyncDisposable.DisposeAsync();

        if(_silaMonitors.Remove(adapter.Id, out var monitor))
          monitor.Dispose();
      }

      foreach(var adapter in newAdapters)
      {
        _ = adapter.Activate();
        var monitor = new SilaDeviceAdapterMonitor(adapter, _loggerFactory.CreateLogger<SilaDeviceAdapterMonitor>());
        _silaMonitors[adapter.Id] = monitor;
        monitor.Activate();
      }

      _silaDeviceAdapterRepository.Edit(updater =>
      {
        updater.AddOrUpdate(newAdapters);
        updater.RemoveKeys(removedDevices);
      });
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Error updating SiLA device adapters.");
    }
  }

  public async ValueTask DisposeAsync()
  {
    await _remoteDeviceCts.CancelAsync();
    if(_remoteDevicePollingTask is not null)
      await _remoteDevicePollingTask;

    await _silaDeviceCts.CancelAsync();
    if(_silaDevicePollingTask is not null)
      await _silaDevicePollingTask;

    foreach(var monitor in _remoteMonitors.Values)
      monitor.Dispose();
    foreach(var monitor in _silaMonitors.Values)
      monitor.Dispose();

    _remoteDeviceCts.Dispose();
    _silaDeviceCts.Dispose();
    GC.SuppressFinalize(this);
  }
}
