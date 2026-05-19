using System.Collections.Concurrent;
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
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote;

public sealed class RemoteDevice : AresDevice, IAsyncDisposable
{
  private readonly GrpcChannel _channel;
  private DeviceCommandDescriptor[] _commands = [];
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateStreamCts = new();
  private readonly ILogger<RemoteDevice> _logger;
  private DevicePollingSettings _pollingSettings = new()
  {
    IntervalMs = 1000,
    PollingType = PollingType.Interval
  };

  public RemoteDevice(RemoteConnectionInfo remoteInfo, ILogger<RemoteDevice> logger) : base(remoteInfo.ConnectionInfo)
  {
    _channel = GrpcChannel.ForAddress(remoteInfo.Address);
    UniqueId = remoteInfo.ConnectionInfo.DeviceId;
    Address = new Uri(remoteInfo.Address);
    _logger = logger;
  }

  public Uri Address { get; }

  public ConcurrentDictionary<string, AresValue> Settings { get; } = new();

  // sets the polling options with the option to restart/start the stream
  public void SetPollingSettings(DevicePollingSettings pollingSettings, bool restartStream = false)
  {
    _pollingSettings = pollingSettings;

    if(restartStream)
    {
      _ = StartStateStream();
    }
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();
  public AresStruct? CurrentState => _stateSubject.Value;
  public AresStructSchema StateSchema { get; private set; } = new();

  public override async Task<List<DeviceCommandDescriptor>> GetCommandDescriptorsAsync()
  {
    return await BuildCommandDescriptorsAsync();
  }

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
      var message = $"Exception In State Stream for Remote Device {Name}: {e.Message}";
      Console.WriteLine(message);
      _logger.LogError(message);
    }
    catch(RpcException e)
    {
      Status = new DeviceOperationalStatus
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"State stream disconnected: {e.Message}" 
      };

      var rpcMessage = $"Exception In State Stream for Remote Device {Name}: {e.Message}";
      Console.WriteLine(rpcMessage);
      _logger.LogError(rpcMessage);
    }
    catch(Exception e)
    {
      Status = new DeviceOperationalStatus
      { 
        OperationalState = OperationalState.Error, 
        Message = $"Unspecified error occurred while fetching device state: {e.Message}" 
      };

      var exception = $"Exception In State Stream for Remote Device {Name}: {e.Message}";
      Console.WriteLine(exception);
      _logger.LogError(exception);
    }
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(_stateSubject.Value ?? new AresStruct());
  }

  internal async Task FetchOperationalStatus()
  {
    try
    {
      var callOpts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
      var client = GetClient();
      var status = await client.GetOperationalStatusAsync(new Empty(), callOpts);
      Status = status;
    }
    catch(RpcException e)
    {
      Status = new DeviceOperationalStatus 
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"Unable to connect to remote device: {e.Message}" 
      };

      _logger.LogWarning("Unable to connect to remote device {device_name}: {error}", Name, e.Message);
    }
  }

  internal async Task FetchCommands()
  {
    var client = GetClient();
    try
    {
      //var callOpts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
      //var cmdResponse = await client.GetCommandsAsync(new Empty(), callOpts);
      //_commands = [.. cmdResponse.Commands];
      await BuildCommandDescriptorsAsync();
    }
    catch(RpcException ex)
    {
      Status = new DeviceOperationalStatus 
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"Failed to fetch commands. Possible connection issue." 
      };

      _logger.LogWarning("Unable to fetch commands. Possible connection issues with remote device {device_name}: {error}", Name, ex.Message);
    }
  }

  internal async Task FetchStateSchema()
  {
    var client = GetClient();
    try
    {
      var response = await client.GetStateSchemaAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(5));
      StateSchema = response.Schema;
    }
    catch(RpcException ex)
    {
      Status = new DeviceOperationalStatus 
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"Failed to fetch state schema. Possible connection issue." 
      };

      _logger.LogWarning("Failed to fetch state schema. Possible connection issues with remote device {device_name}: {error}", Name, ex.Message);
    }
  }

  internal async Task FetchInfo()
  {
    var client = GetClient();
    try
    {
      var callOpts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
      var info = await client.GetInfoAsync(new Empty(), callOpts);
      Type = info.Name;
      Version = info.Version;
      Description = info.Description;
    }
    catch(RpcException ex)
    {
      Status = new DeviceOperationalStatus 
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"Failed to fetch info. Possible connection issue." 
      };

      _logger.LogWarning("Failed to fetch device info. Possible connection issues with remote device {device_name}: {exception}", Name, ex.Message);
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    var client = GetClient();
    _logger.LogCritical("Remote Device {device_name} is entering safe mode!", Name);
    return client.EnterSafeModeAsync(new Empty()).ResponseAsync;
  }

  public async override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    var client = GetClient();
    var executionRequest = new ExecuteCommandRequest { CommandName = command, Arguments = new AresStruct() };
    foreach(var argument in arguments)
    {
      executionRequest.Arguments.Fields[argument.ArgName] = argument.ArgValue;
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
    {;
      var response = await client.GetSettingsSchemaAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(5));

      if(response.Schema is not null)
        SettingSchema = response.Schema;
    }
    catch(RpcException ex)
    {
      Status = new DeviceOperationalStatus 
      { 
        OperationalState = OperationalState.Inactive, 
        Message = $"Failed to fetch settings. Possible connection issue." 
      };

      _logger.LogError("Caught an RPC Exception Fetching Operational Status for {Name}: {ex.Message}", Name, ex.Message);
    }

    try
    {
      var response = await client.GetCurrentSettingsAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(5));

      if(response.Settings is not null)
        await UpdateSettings(response.Settings);
    }
    catch(RpcException ex)
    {
      _logger.LogWarning("Failed to fetch current settings for remote device {device_name}: {error}", Name, ex.Message);
    }

    var newSettings = SettingSchema.Fields.Where(entry => !Settings.ContainsKey(entry.Key)).ToArray();
    var removedSettings = Settings.Where(entry => !SettingSchema.Fields.ContainsKey(entry.Key)).ToArray();

    foreach(var removedSetting in removedSettings)
      Settings.Remove(removedSetting.Key, out _);

    foreach(var newSetting in newSettings)
    {
      if(newSetting.Value.Type == AresDataType.String)
        Settings[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type, newSetting.Value.StringChoices?.Strings);

      else if(newSetting.Value.Type == AresDataType.Number)
      {
        Settings[newSetting.Key] = AresValueHelper.CreateDefault(
          newSetting.Value.Type,
          newSetting.Value.NumberChoices?.Numbers);
      }

      else
      {
        Settings[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type);
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

  public async override Task UpdateSettings(AresStruct settings)
  {
    foreach(var setting in Settings)
    {
      var newValue = settings.Fields.GetValueOrDefault(setting.Key);
      if(newValue is null)
        continue;

      Settings[setting.Key] = newValue;
    }

    var aresSettings = new AresStruct();
    aresSettings.Fields.Add(Settings);

    var client = GetClient();
    await client.SetSettingsAsync(new SetSettingsRequest { Settings = aresSettings }, deadline: DateTime.UtcNow.AddSeconds(5));    
  }

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

  public override async Task<AresStruct> GetSettings()
  {
    var client = GetClient();
    var response = await client.GetCurrentSettingsAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(5));
    return response.Settings;
  }

  protected override async Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    var client = GetClient();
    try
    {
      var callOpts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
      var cmdResponse = await client.GetCommandsAsync(new Empty(), callOpts);
      _commands = [.. cmdResponse.Commands];
      return _commands.ToList();
    }
    catch(RpcException ex)
    {
      Status = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Inactive,
        Message = $"Failed to fetch commands. Possible connection issue."
      };

      _logger.LogWarning("Failed to fetch commands for remote device {device_name}: {error}", Name, ex.Message);
      return [];
    }
  }
}
