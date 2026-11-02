using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Remote;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using UI.Application.Devices;

namespace UI.Infrastructure.Devices.Remote;

public sealed class RemoteDeviceAdapter : IAresDeviceAdapter, IAsyncDisposable
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly BehaviorSubject<ConnectionStatus> _connectionStatusSubject = new(ConnectionStatus.Undefined);
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly ILogger<RemoteDeviceAdapter> _logger;
  private CancellationTokenSource _statusStreamCts = new();
  private IDisposable? _stateSubscription;

  public RemoteDeviceAdapter(IAresDeviceProvider deviceProvider,
    string id,
    ILogger<RemoteDeviceAdapter> logger)
  {
    _deviceProvider = deviceProvider;
    _logger = logger;
    Id = id;
    OperationalStatus = new DeviceOperationalStatus
    {
      OperationalState = OperationalState.Unspecified,
      Message = "No information yet from ARES service."
    };
  }

  public string Id { get; }
  public string Name { get; private set; } = "";
  public string Type { get; private set; } = "";
  public string Description { get; private set; } = "";
  public string Version { get; private set; } = "";
  public bool Active { get; private set; }
  public IObservable<AresStruct?> StateStream => _stateSubject.AsObservable();
  public AresStruct? State => _stateSubject.Value;
  public DeviceOperationalStatus OperationalStatus { get; private set; }
  public IObservable<ConnectionStatus> ConnectionStatusStream => _connectionStatusSubject.AsObservable();
  public AresStructSchema? StateSchema { get; private set; }

  public async Task<bool> Activate()
  {
    await FetchInfo();
    await FetchStateSchema();
    await StartStateStream();
    await StartStatusUpdate();
    return true;
  }

  public Task UpdateConnectionStatus() => FetchOperationalStatus();

  public async Task StartStatusUpdate()
  {
    await _statusStreamCts.CancelAsync();
    _statusStreamCts = new CancellationTokenSource();
    var token = _statusStreamCts.Token;

    _ = Task.Run(async () =>
    {
      while(!token.IsCancellationRequested)
      {
        await UpdateConnectionStatus();
        await Task.Delay(TimeSpan.FromSeconds(5), token);
      }
    }, token);
  }

  public Task StartStateStream()
  {
    _stateSubscription?.Dispose();

    var device = GetDevice();
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

  public async Task<IReadOnlyList<DeviceCommandDescriptor>> GetCommandDescriptorsAsync(
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await GetDevice().GetCommandDescriptorsAsync();
  }

  public Task<CommandResult> ExecuteCommandAsync(
    string commandName,
    IReadOnlyCollection<DeviceCommandArgument> arguments,
    CancellationToken cancellationToken = default)
  {
    return GetDevice().ExecuteCommand(commandName, arguments.ToList(), cancellationToken);
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var status = await GetDevice().StatusObservable.FirstAsync();
      _logger.LogInformation("Fetched operational status for device {name}.", Name);
      OperationalStatus = status;
      UpdateStatusIfChanged(status.OperationalState == OperationalState.Active
        ? ConnectionStatus.ConnectedToDevice
        : ConnectionStatus.ConnectedToService);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to fetch operational status for device {name}.", Name);
      OperationalStatus = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Unspecified,
        Message = "Unknown operational state. Trouble connecting to ARES service."
      };
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }
  }

  internal Task FetchStateSchema()
  {
    try
    {
      StateSchema = GetDevice().StateSchema;
      _logger.LogInformation("Fetched state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to fetch state schema for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }

    return Task.CompletedTask;
  }

  internal Task FetchInfo()
  {
    try
    {
      var device = GetDevice();
      Name = device.Name;
      Version = device.Version;
      Description = device.Description;
      Type = device.Type;
      _logger.LogInformation("Fetched device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.ConnectedToService);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to fetch device info for device {name}.", Name);
      UpdateStatusIfChanged(ConnectionStatus.Disconnected);
    }

    return Task.CompletedTask;
  }

  private void UpdateStatusIfChanged(ConnectionStatus status)
  {
    var currentStatus = _connectionStatusSubject.Value;
    if(OperationalStatus.OperationalState == OperationalState.Active)
      status = ConnectionStatus.ConnectedToDevice;

    if(currentStatus != status)
      _connectionStatusSubject.OnNext(status);
  }

  public async ValueTask DisposeAsync()
  {
    _logger.LogInformation("Disposing remote device adapter for device {name}.", Name);
    _stateSubscription?.Dispose();
    await _statusStreamCts.CancelAsync();
    _statusStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
    _connectionStatusSubject.OnCompleted();
    _connectionStatusSubject.Dispose();
  }

  private RemoteDevice GetDevice()
  {
    return _deviceProvider.GetDevice<RemoteDevice>(Id)
      ?? throw new InvalidOperationException($"Device '{Id}' is no longer available.");
  }
}
