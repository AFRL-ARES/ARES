using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;

namespace UI.Backend.Devices;
public sealed class RemoteDeviceAdapter : IAresDeviceAdapter, IAsyncDisposable
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly BehaviorSubject<ConnectionStatus> _connectionStatusSubject =
    new BehaviorSubject<ConnectionStatus>(ConnectionStatus.Undefined);
  
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly ILogger<RemoteDeviceAdapter> _logger;
  private CancellationTokenSource _stateStreamCts = new();
  private DevicePollingSettings _pollingSettings = new()
  {
    IntervalMs = 1000,
    PollingType = PollingType.Interval
  };


  public RemoteDeviceAdapter(AresDevices.AresDevicesClient devicesClient, string id, ILogger<RemoteDeviceAdapter> logger)
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
  public AresDataSchema? StateSchema { get; private set; }

  public async Task<bool> Activate()
  {
    await FetchInfo();
    _ = StartStateStream();
    return true;
  }

  public async Task StartStateStream()
  {
    await _stateStreamCts.CancelAsync();
    _stateStreamCts = new CancellationTokenSource();
    var token = _stateStreamCts.Token;
    Active = true;
    try
    {
      using var call = _devicesClient.GetDeviceStateStream(new DeviceStateStreamRequest { DeviceId = Id, PollingSettings = _pollingSettings });
      _logger.LogInformation("Started device state stream for device {}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Connected);
      await foreach(var state in call.ResponseStream.ReadAllAsync(token))
      {
        _stateSubject.OnNext(state.State);
      }
    }
    catch(Exception e)
    {
      _logger.LogError("Device stream for {name} has stopped. {ex}", Name, e.Message);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }

    Active = false;
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = Id });
      _logger.LogInformation("Fetched operational status for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Connected);
      OperationalStatus = status;
    }
    catch(RpcException e)
    {
      _logger.LogError("Failed to fetch operational status for device {name}. {ex}", Name, e.Message);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  internal async Task FetchStateSchema()
  {
    try
    {
      var response = await _devicesClient.GetDeviceStateSchemaAsync(new DeviceStateSchemaRequest { DeviceId = Id });
      _logger.LogInformation("Fetched state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Connected);
      StateSchema = response.Schema;
    }
    catch(RpcException)
    {
      _logger.LogError("Failed to fetch state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  internal async Task FetchInfo()
  {
    try
    {
      var info = await _devicesClient.GetDeviceInfoAsync(new DeviceInfoRequest { DeviceId = Id });
      _logger.LogInformation("Fetched device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Connected);
      Name = info.Name;
      Version = info.Version;
      Description = info.Description;
      Type = info.Type;
    }
    catch(RpcException)
    {
      _logger.LogError("Failed to fetch device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  private void UpdateStatusIfChanged(ConnectionStatus status)
  {
    var currentStatus = _connectionStatusSubject.Value;
    if (currentStatus != status)
    {
      _connectionStatusSubject.OnNext(status);
    }
  }

  public async ValueTask DisposeAsync()
  {
    _logger.LogInformation("Disposing RemoteDeviceAdapter for device {name}.", Name);
    await _stateStreamCts.CancelAsync();
    _stateStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }
}
