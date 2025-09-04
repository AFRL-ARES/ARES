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
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private CancellationTokenSource _stateStreamCts = new();
  private DevicePollingSettings _pollingSettings = new()
  {
    IntervalMs = 1000,
    PollingType = PollingType.Interval
  };

  public RemoteDeviceAdapter(AresDevices.AresDevicesClient devicesClient, string id)
  {
    _devicesClient = devicesClient;
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
  public AresStruct? State => _stateSubject.Value;
  public DeviceOperationalStatus OperationalStatus { get; private set; }
  public AresDataSchema? StateSchema { get; private set; }

  public async Task<bool> Activate()
  {
    //await FetchOperationalStatus();
    //if(OperationalStatus.OperationalState != OperationalState.Active)
    //{
    //  return false;
    //}
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
      await foreach(var state in call.ResponseStream.ReadAllAsync(token))
      {
        _stateSubject.OnNext(state.State);
      }
    }
    catch(Exception e)
    {
      OperationalStatus = new DeviceOperationalStatus
      { OperationalState = OperationalState.Error, Message = $"Unspecified error occurred. Ares Service possibly lost connection: {e.Message}" };
    }

    Active = false;
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = Id });
      OperationalStatus = status;
    }
    catch(RpcException e)
    {
      OperationalStatus = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Unable to connect to Ares Service: {e.Message}" };
    }
  }

  internal async Task FetchStateSchema()
  {
    try
    {
      var response = await _devicesClient.GetDeviceStateSchemaAsync(new DeviceStateSchemaRequest { DeviceId = Id });
      StateSchema = response.Schema;
    }
    catch(RpcException)
    {
      OperationalStatus = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch state schema. Possible connection issue with Ares Service." };
    }
  }

  internal async Task FetchInfo()
  {
    try
    {
      var info = await _devicesClient.GetDeviceInfoAsync(new DeviceInfoRequest { DeviceId = Id });
      Name = info.Name;
      Version = info.Version;
      Description = info.Description;
      Type = info.Type;
    }
    catch(RpcException)
    {
      OperationalStatus = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch info. Possible connection issue." };
    }
  }

  public async ValueTask DisposeAsync()
  {
    await _stateStreamCts.CancelAsync();
    _stateStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }
}
