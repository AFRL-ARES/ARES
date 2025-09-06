using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Device.Remote;
using Ares.Datamodel.Extensions;
using Ares.Device;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Ares.Core.Device.Remote;
public sealed class RemoteDevice : AresDevice, IAsyncDisposable
{
  private readonly GrpcChannel _channel;
  private DeviceCommandDescriptor[] _commands = [];
  private readonly BehaviorSubject<AresStruct?> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateStreamCts = new();
  private DevicePollingSettings _pollingSettings = new()
  {
    IntervalMs = 1000,
    PollingType = PollingType.Interval
  };

  public RemoteDevice(string name, Uri address) : base(name)
  {
    _channel = GrpcChannel.ForAddress(address);
    Address = address;
  }

  public RemoteDevice(string name, Uri address, string id) : base(name)
  {
    _channel = GrpcChannel.ForAddress(address);
    UniqueId = id;
    Address = address;
  }

  public Uri Address { get; }

  public AresStruct Settings { get; } = new();
  
  public AresDataSchema SettingSchema { get; private set; } = new();

  // sets the polling options with the option to restart/start the stream
  public void SetPollingSettings(DevicePollingSettings pollingSettings, bool restartStream = false)
  {
    _pollingSettings = pollingSettings;

    if(restartStream)
    {
      _ = StartStateStream();
    }
  }

  public IObservable<AresStruct?> StateStream => _stateSubject.AsObservable();
  public AresStruct? CurrentState => _stateSubject.Value;
  public AresDataSchema StateSchema { get; private set; } = new();

  public override async Task<bool> Activate(CancellationToken ct)
  {
    await FetchOperationalStatus();
    if(Status.OperationalState != OperationalState.Active)
    {
      return false;
    }
    await FetchInfo();
    await FetchCommands();
    await FetchSettings();
    await FetchStateSchema();
    _ = StartStateStream();
    return true;
  }

  public async Task StopStateStream()
  {
    await _stateStreamCts.CancelAsync();
    _stateStreamCts = new CancellationTokenSource();
  }

  public async Task StartStateStream()
  {
    var token = _stateStreamCts.Token;
    var client = GetClient();
    try
    {
      using var call =
        client.GetStateStream(new DeviceStateStreamRequest { PollingSettings = _pollingSettings }, cancellationToken: token);
      await foreach(var state in call.ResponseStream.ReadAllAsync(token))
      {
        _stateSubject.OnNext(state.State);
      }
    }
    catch(RpcException e) when(e.StatusCode == StatusCode.Cancelled)
    {
    }
    catch(RpcException e)
    {
      Status = new DeviceOperationalStatus
      { OperationalState = OperationalState.Inactive, Message = $"State stream disconnected: {e.Message}" };
    }
    catch(Exception e)
    {
      Status = new DeviceOperationalStatus
      { OperationalState = OperationalState.Error, Message = $"Unspecified error occurred while fetching device state: {e.Message}" };
    }
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var client = GetClient();
      var status = await client.GetOperationalStatusAsync(new Empty());
      Status = status;
    }
    catch(RpcException e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Unable to connect to remote device: {e.Message}" };
    }
  }

  internal async Task FetchCommands()
  {
    var client = GetClient();
    try
    {
      var cmdResponse = await client.GetCommandsAsync(new Empty());
      _commands = [.. cmdResponse.Commands];
    }
    catch(RpcException)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch commands. Possible connection issue." };
    }
  }

  internal async Task FetchStateSchema()
  {
    var client = GetClient();
    try
    {
      var response = await client.GetStateSchemaAsync(new Empty());
      StateSchema = response.Schema;
    }
    catch(RpcException)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch state schema. Possible connection issue." };
    }
  }

  internal async Task FetchInfo()
  {
    var client = GetClient();
    try
    {
      var info = await client.GetInfoAsync(new Empty());
      Type = info.Name;
      Version = info.Version;
      Description = info.Description;
    }
    catch(RpcException)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch info. Possible connection issue." };
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    var client = GetClient();
    return client.EnterSafeModeAsync(new Empty()).ResponseAsync;
  }

  public async Task<CommandResult> ExecuteCommand(string command, AresStruct arguments, CancellationToken token)
  {
    var client = GetClient();
    var executionRequest = new ExecuteCommandRequest { CommandName = command };
    foreach(var argument in arguments.Fields)
    {
      executionRequest.Arguments[argument.Key] = argument.Value;
    }

    var executionResult = await client.ExecuteCommandAsync(executionRequest, cancellationToken: token);

    var cmdResult = new CommandResult
    {
      Success = executionResult.Success,
      Result = executionResult.Result,
      Error = executionResult.Error
    };

    return cmdResult;
  }

  internal async Task FetchSettings()
  {
    var client = GetClient();
    try
    {
      var response = await client.GetSettingsSchemaAsync(new Empty());
      SettingSchema = response.Schema;
    }
    catch(RpcException)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = $"Failed to fetch settings. Possible connection issue." };
    }

    try
    {
      var response = await client.GetCurrentSettingsAsync(new Empty());
      await UpdateSettings(response.Settings);
    }
    catch(RpcException)
    {
    }

    var newSettings = SettingSchema.Fields.Where(entry => !Settings.Fields.ContainsKey(entry.Key)).ToArray();
    var removedSettings = Settings.Fields.Where(entry => !SettingSchema.Fields.ContainsKey(entry.Key)).ToArray();

    foreach(var removedSetting in removedSettings)
    {
      Settings.Fields.Remove(removedSetting.Key);
    }

    foreach(var newSetting in newSettings)
    {
      if(newSetting.Value.Type == AresDataType.String)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type, newSetting.Value.StringChoices?.Strings);
      }
      else if(newSetting.Value.Type == AresDataType.Number)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(
          newSetting.Value.Type,
          newSetting.Value.NumberChoices?.Numbers);
      }
      else
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type);
      }
    }
  }

  internal async Task UpdateInfo(DeviceInfo info)
  {
    Type = info.Type;
    Description = info.Description;
    Version = info.Version;
    SettingSchema = info.SettingsSchema;
    _commands = [.. info.Commands];
    await FetchSettings();
  }

  public Task UpdateSettings(AresStruct settings)
  {
    foreach(var setting in Settings.Fields)
    {
      var newValue = settings.Fields.GetValueOrDefault(setting.Key);
      if(newValue is null)
      {
        continue;
      }

      Settings.Fields[setting.Key] = newValue;
    }

    var client = GetClient();
    return client.SetSettingsAsync(new SetSettingsRequest { Settings = Settings }).ResponseAsync;
  }

  public IReadOnlyList<DeviceCommandDescriptor> CommandDescriptors => _commands;

  private AresRemoteDeviceService.AresRemoteDeviceServiceClient GetClient()
  {
    return new AresRemoteDeviceService.AresRemoteDeviceServiceClient(_channel);
  }

  public async ValueTask DisposeAsync()
  {
    await _stateStreamCts.CancelAsync();
    _stateStreamCts.Dispose();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
    await _channel.ShutdownAsync();
    _channel.Dispose();
  }
}
