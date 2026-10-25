using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Sila;
using UI.Infrastructure.Devices.Remote;
using UI.Infrastructure.Devices.Sila;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices;

public class DeviceAdapterManager : IAsyncDisposable
{
  private readonly DevicesService _devicesClient;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IRemoteDeviceAdapterRepository _remoteDeviceAdapterRepository;
  private readonly ISilaDeviceAdapterRepository _silaDeviceAdapterRepository;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<DeviceAdapterManager> _logger;
  private readonly CancellationTokenSource _remoteDeviceCTS = new();
  private readonly CancellationTokenSource _silaDeviceCTS = new();
  private Task? _remoteDevicePollingTask;
  private Task? _silaDevicePollingTask;
  private bool _isErrorState;
  private IDictionary<string, RemoteDeviceAdapterMonitor> _remoteMonitors = new Dictionary<string, RemoteDeviceAdapterMonitor>();
  private IDictionary<string, SilaDeviceAdapterMonitor> _silaMonitors = new Dictionary<string, SilaDeviceAdapterMonitor>();

  public DeviceAdapterManager(DevicesService devicesClient, 
    IAresDeviceProvider deviceProvider, 
    IRemoteDeviceAdapterRepository remoteAdapterRepo, 
    ISilaDeviceAdapterRepository silaAdapterRepo, 
    ILoggerFactory loggerFactory, 
    ILogger<DeviceAdapterManager> logger)
  {
    _devicesClient = devicesClient;
    _deviceProvider = deviceProvider;
    _remoteDeviceAdapterRepository = remoteAdapterRepo;
    _silaDeviceAdapterRepository = silaAdapterRepo;
    _loggerFactory = loggerFactory;
    _logger = logger;
  }

  public void Activate()
  {
    _remoteDevicePollingTask = Task.Run(() => PollRemoteDevicesAsync(_remoteDeviceCTS.Token));
    _silaDevicePollingTask = Task.Run(() => PollSilaDevicesAsync(_silaDeviceCTS.Token));
  }

  private async Task PollRemoteDevicesAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        // let's stick to remote devices for now as the built-int devices have their
        // own logic in viewmodels
        var devices = await _devicesClient.ListRemoteAresDevices(new Empty(), null);

        if (_isErrorState)
        {
          _logger.LogInformation("Device polling recovered.");
          _isErrorState = false;
        }

        await UpdateAdaptersFromDeviceList(devices);
      }
      catch (OperationCanceledException)
      {
        // This is expected on shutdown.
        break;
      }
      catch (Exception ex)
      {
        if (!_isErrorState)
        {
          _logger.LogError(ex, "Error polling remote Ares devices.");
          _isErrorState = true;
        }
      }

      try
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
      catch (OperationCanceledException)
      {
        // This is expected on shutdown.
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
        // This is expected on shutdown.
        break;
      }
      catch(Exception ex)
      {
        if(!_isErrorState)
        {
          _logger.LogError(ex, "Error polling remote Ares devices.");
          _isErrorState = true;
        }
      }

      try
      {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
      catch(OperationCanceledException)
      {
        // This is expected on shutdown.
        break;
      }
    }
  }

  private async Task UpdateAdaptersFromDeviceList(ListAresRemoteDevicesResponse devicesResponse)
  {
    try
    {
      var remoteIds = devicesResponse.Devices.Select(d => d.UniqueId).ToHashSet();
      var existingIds = _remoteDeviceAdapterRepository.Keys.ToHashSet();

      var newDevices = remoteIds.Except(existingIds);
      var removedDevices = existingIds.Except(remoteIds);

      var newAdapters = newDevices
              .Select(id => new RemoteDeviceAdapter(_devicesClient, id, _loggerFactory.CreateLogger<RemoteDeviceAdapter>())).ToArray();

      var removedAdapters = _remoteDeviceAdapterRepository.Items.Where(da => removedDevices.Contains(da.Id)).ToArray();
      foreach (var adapter in removedAdapters)
      {
        if (adapter is IAsyncDisposable asyncDisposable)
        {
          await asyncDisposable.DisposeAsync();
        }
        if (_remoteMonitors.Remove(adapter.Id, out var monitor))
        {
          monitor.Dispose();
        }
      }

      foreach (var adapter in newAdapters)
      {
        _ = adapter.Activate();
        var monitor = new RemoteDeviceAdapterMonitor(adapter, _loggerFactory.CreateLogger<RemoteDeviceAdapterMonitor>());
        _remoteMonitors[adapter.Id] = monitor;
      }

      _remoteDeviceAdapterRepository.Edit(updater =>
      {
        updater.AddOrUpdate(newAdapters); // add/update
        updater.RemoveKeys(removedDevices);   // remove stale
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating device adapter repository.");
    }
  }

  private async Task UpdateSilaAdaptersFromDeviceList(IReadOnlyCollection<SilaDevice> silaDevices)
  {
    try
    {
      var silaDeviceIds = silaDevices.Select(d => d.UniqueId).ToHashSet();
      var existingIds = _silaDeviceAdapterRepository.Keys.ToHashSet();

      var newDevices = silaDeviceIds.Except(existingIds);
      var removedDevices = existingIds.Except(silaDeviceIds);

      var newAdapters = newDevices.Select(id => new SilaDeviceAdapter(_deviceProvider, id, _loggerFactory.CreateLogger<SilaDeviceAdapter>())).ToArray();

      var removedAdapters = _silaDeviceAdapterRepository.Items.Where(da => removedDevices.Contains(da.Id)).ToArray();
      foreach(var adapter in removedAdapters)
      {
        if(adapter is IAsyncDisposable asyncDisposable)
        {
          await asyncDisposable.DisposeAsync();
        }
        if(_silaMonitors.Remove(adapter.Id, out var monitor))
        {
          monitor.Dispose();
        }
      }

      foreach(var adapter in newAdapters)
      {
        _ = adapter.Activate();
        var monitor = new SilaDeviceAdapterMonitor(adapter, _loggerFactory.CreateLogger<SilaDeviceAdapterMonitor>());
        _silaMonitors[adapter.Id] = monitor;
      }

      _silaDeviceAdapterRepository.Edit(updater =>
      {
        updater.AddOrUpdate(newAdapters);
        updater.RemoveKeys(removedDevices);
      });
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Error updating device adapter repository.");
    }
  }

  public async ValueTask DisposeAsync()
  {
    await _remoteDeviceCTS.CancelAsync();
    if(_remoteDevicePollingTask is not null)
      await _remoteDevicePollingTask;

    if(_silaDevicePollingTask is not null)
      await _silaDevicePollingTask;
    
    _remoteDeviceCTS.Dispose();
    _silaDeviceCTS.Dispose();
    GC.SuppressFinalize(this);
  }
}
