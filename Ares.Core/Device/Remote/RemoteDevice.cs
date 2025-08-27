using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Device.Remote;
using Ares.Datamodel.Extensions;
using Ares.Device;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Ares.Core.Device.Remote;
public sealed class RemoteDevice : AresDevice
{
  private readonly GrpcChannel _channel;
  private AresDataSchema _settingsSchema = new();
  private DeviceCommandDescriptor[] _commands = [];

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

  public AresDataSchema SettingSchema => _settingsSchema;

  public override async Task<bool> Activate()
  {
    await FetchOperationalStatus();
    if(Status.OperationalState != Datamodel.Device.OperationalState.Active)
    {
      return false;
    }
    await FetchInfo();
    await FetchCommands();
    await FetchSettings();
    return true;
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
    }
  }

  public override Task EnterSafeMode()
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
      _settingsSchema = response.Schema;
    }
    catch(RpcException)
    {
    }

    try
    {
      var response = await client.GetCurrentSettingsAsync(new Empty());
      await UpdateSettings(response.Settings);
    }
    catch(RpcException)
    {
    }

    var newSettings = _settingsSchema.Fields.Where(entry => !Settings.Fields.ContainsKey(entry.Key)) ?? [];
    var removedSettings = Settings.Fields.Where(entry => !_settingsSchema.Fields.ContainsKey(entry.Key));

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
    _settingsSchema = info.SettingsSchema;
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
}
