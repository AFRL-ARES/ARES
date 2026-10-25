using Ares.Core.Device.Providers;
using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UI.Application.Devices;

namespace UI.Infrastructure.Devices.Sila;

public sealed class SilaDeviceAdapter : IAresDeviceAdapter, IAsyncDisposable
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly BehaviorSubject<ConnectionStatus> _connectionStatusSubject = new BehaviorSubject<ConnectionStatus>(ConnectionStatus.Undefined);
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly ILogger<SilaDeviceAdapter> _logger;
  private CancellationTokenSource _statusStreamCts = new();
  private IDisposable? _stateSubscription;

  public SilaDeviceAdapter(IAresDeviceProvider deviceProvider,
    string id, 
    ILogger<SilaDeviceAdapter> logger)
  {
    _deviceProvider = deviceProvider;
    _logger = logger;
    Id = id;
    Address = "";
    OperationalStatus = new DeviceOperationalStatus() { OperationalState = OperationalState.Unspecified, Message = "Operational State Not Specified" };
  }

  public async Task<bool> Activate()
  {
    await FetchInfo();
    await StartStateStream();
    await StartStatusUpdate();
    return true;
  }

  public Task UpdateConnectionStatus()
  {
    // Fetching operational status is the least amount of data and it still updates the connection status
    return FetchOperationalStatus();
  }

  public async Task StartStatusUpdate()
  {
    await _statusStreamCts.CancelAsync();
    _statusStreamCts = new CancellationTokenSource();
    var token = _statusStreamCts.Token;
    _ = Task.Run(async () =>
    {
      while(!_statusStreamCts.IsCancellationRequested)
      {
        await UpdateConnectionStatus();
        await Task.Delay(TimeSpan.FromSeconds(5));
      }
    }, _statusStreamCts.Token);
  }

  public Task StartStateStream()
  {
    _stateSubscription?.Dispose();

    var device = _deviceProvider.GetDevice<SilaDevice>(Id);
    if(device is null)
    {
      Active = false;
      _logger.LogError("Failed to start state stream for SiLA device {id}, as the provider could not locate it.", Id);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
      return Task.CompletedTask;
    }

    _stateSubscription = device.StateStream
      .DistinctUntilChanged()
      .Subscribe(
        state => _stateSubject.OnNext(state),
        ex =>
        {
          Active = false;
          _logger.LogError(ex, "Device state stream for {name} has stopped.", Name);
          UpdateStatusIfChanged(ConnectionStatus.Disconnected);
        },
        () => Active = false);

    Active = true;
    _logger.LogInformation("Started device state stream for device {name}.", Name);
    UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
    return Task.CompletedTask;
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var device = _deviceProvider.GetDevice<SilaDevice>(Id);

      if(device is null)
      {
        _logger.LogError("Failed to fetch operational status for Sila Device, as provider failed to locate it with an ID of {id}", Id);
        return;
      }

      var status = await device.StatusObservable.FirstAsync();

      _logger.LogInformation("Fetched operational status for device {name}.", Name);

      if(status.OperationalState == OperationalState.Active)
        UpdateStatusIfChanged(ConnectionStatus.ConnectedToDevice);

      else
        UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);

      OperationalStatus = status;
    }
    catch(Exception e)
    {
      _logger.LogError("Failed to fetch operational status for device {name}. {ex}", Name, e.Message);
      OperationalStatus = new DeviceOperationalStatus { OperationalState = OperationalState.Unspecified, Message = "Unknown operational state. Trouble connecting to ARES service." };
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  internal async Task FetchStateSchema()
  {
    try
    {
      var device = _deviceProvider.GetDevice<SilaDevice>(Id);

      if(device is null)
      {
        _logger.LogError("Failed to fetch State Schema for Sila Device, as provider failed to locate it with an ID of {id}", Id);
        return;
      }

      StateSchema = device?.StateSchema;
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);

      _logger.LogInformation("Fetched state schema for device {name}.", Name);
    }
    catch(Exception)
    {
      _logger.LogError("Failed to fetch state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  internal async Task FetchInfo()
  {
    try
    {
      var device = _deviceProvider.GetDevice<SilaDevice>(Id);

      if(device is null)
      {
        _logger.LogError("Failed to fetch status for Sila Device, as provider failed to locate it with an ID of {id}", Id);
        return;
      }

      _logger.LogInformation("Fetched device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
      Name = device.Name;
      Version = device.Version;
      Description = device.Description;
      Type = device.Type;
      Address = device.Address?.ToString() ?? string.Empty;
    }
    catch(Exception)
    {
      _logger.LogError("Failed to fetch device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  private void UpdateStatusIfChanged(ConnectionStatus status)
  {
    var currentStatus = _connectionStatusSubject.Value;
    var deviceActive = OperationalStatus.OperationalState == OperationalState.Active;
    status = deviceActive ? ConnectionStatus.ConnectedToDevice : status;
    if(currentStatus != status)
      _connectionStatusSubject.OnNext(status);
  }

  public async ValueTask DisposeAsync()
  {
    _logger.LogInformation("Disposing SiLA device adapter for device {name}.", Name);
    _stateSubscription?.Dispose();
    await _statusStreamCts.CancelAsync();
    _statusStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }

  public string Id { get; }
  public string Name { get; private set; } = "";
  public string Type { get; private set; } = "";
  public string Description { get; private set; } = "";
  public string Version { get; private set; } = "";
  public bool Active { get; private set; }
  public string Address { get; private set; }
  public IObservable<AresStruct?> StateStream => _stateSubject.AsObservable();
  public AresStruct? State => _stateSubject.Value;
  public DeviceOperationalStatus OperationalStatus { get; private set; }
  public IObservable<ConnectionStatus> ConnectionStatusStream => _connectionStatusSubject.AsObservable();
  public AresStructSchema? StateSchema { get; private set; }
}
