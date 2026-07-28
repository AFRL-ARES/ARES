using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using UI.Application.Devices;
using UI.Infrastructure.Grpc;

namespace UI.Infrastructure.Devices;
public sealed class RemoteDeviceAdapter : IAresDeviceAdapter, IAsyncDisposable
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly BehaviorSubject<ConnectionStatus> _connectionStatusSubject =
    new BehaviorSubject<ConnectionStatus>(ConnectionStatus.Undefined);

  private readonly DevicesService _devicesClient;
  private readonly ILogger<RemoteDeviceAdapter> _logger;
  private CancellationTokenSource _stateStreamCts = new();
  private CancellationTokenSource _statusStreamCts = new();
  private DevicePollingSettings _pollingSettings = new()
  {
    IntervalMs = 1000,
    PollingType = PollingType.Interval
  };


  public RemoteDeviceAdapter(DevicesService devicesClient, string id, ILogger<RemoteDeviceAdapter> logger)
  {
    _devicesClient = devicesClient;
    _logger = logger;
    Id = id;
    OperationalStatus = new DeviceOperationalStatus
    {
      OperationalState = OperationalState.Unspecified,
      Message = "No information yet from Ares Service yet"
    };
  }

  public string Id { get; }
  public string Name { get; private set; } = "";
  public string Type { get; private set; } = "";
  public string Description { get; private set; } = "";
  public string Version { get; private set; } = "";
  public bool Active { get; private set; }
  public IObservable<AresStruct?> StateStream => _stateSubject.AsObservable();
  public Task UpdateConnectionStatus()
  {
    // Fetching operational status is the least amount of data and it still updates the connection status
    return FetchOperationalStatus();
  }

  public AresStruct? State => _stateSubject.Value;
  public DeviceOperationalStatus OperationalStatus { get; private set; }
  public IObservable<ConnectionStatus> ConnectionStatusStream => _connectionStatusSubject.AsObservable();
  public AresStructSchema? StateSchema { get; private set; }

  public async Task<bool> Activate()
  {
    await FetchInfo();
    await StartStateStream();
    await StartStatusUpdate();
    return true;
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

  public async Task StartStateStream()
  {
    await _stateStreamCts.CancelAsync();
    _stateStreamCts = new CancellationTokenSource();
    var token = _stateStreamCts.Token;
    Active = true;
    _ = Task.Run(async () =>
    {
      try
      {
        var streamWriter = new LocalStreamWriter<DeviceStateResponse>(state => 
        {
            _stateSubject.OnNext(state.State);
            return Task.CompletedTask;
        });

        _logger.LogInformation("Started device state stream for device {name}.", Name);
        UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);

        await _devicesClient.GetDeviceStateStream(new DeviceStateStreamRequest { DeviceId = Id, PollingSettings = _pollingSettings }, streamWriter, null);
      }
      catch(Exception e)
      {
        _logger.LogError("Device stream for {name} has stopped. {ex}", Name, e.Message);
        UpdateStatusIfChanged(ConnectionStatus.Disconnected);
      }
    }, _stateStreamCts.Token);

    Active = false;
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatus(new DeviceStatusRequest { DeviceId = Id }, null);
      _logger.LogInformation("Fetched operational status for device {name}.", Name);
      if(status.OperationalState == OperationalState.Active)
      {
        UpdateStatusIfChanged(ConnectionStatus.ConnectedToDevice);
      }
      else
      {
        UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
      }
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
      var response = await _devicesClient.GetDeviceStateSchema(new DeviceStateSchemaRequest { DeviceId = Id }, null);
      _logger.LogInformation("Fetched state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
      StateSchema = response.Schema;
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
      var info = await _devicesClient.GetDeviceInfo(new DeviceInfoRequest { DeviceId = Id }, null);
      _logger.LogInformation("Fetched device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
      Name = info.Name;
      Version = info.Version;
      Description = info.Description;
      Type = info.Type;
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
    {
      _connectionStatusSubject.OnNext(status);
    }
  }

  public async ValueTask DisposeAsync()
  {
    _logger.LogInformation("Disposing RemoteDeviceAdapter for device {name}.", Name);
    await _stateStreamCts.CancelAsync();
    _stateStreamCts.Dispose();
    await _statusStreamCts.CancelAsync();
    _statusStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }
}
