using System.Threading;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.Managers;
using Ares.Core.Device.Remote;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Ares.Core.Device.Providers;

namespace Ares.Core.Grpc.Services;

public class DevicesService(
  IAresDeviceProvider deviceProvider,
  IAresDriverProvider driverProvider,
  IDeviceManager deviceManager,
  IDeviceConfigManager deviceConfigManager,
  IDbContextFactory<CoreDatabaseContext> contextFactory,
  IRemoteDeviceManager remoteDeviceManager,
  StateLoggerManager _stateLoggerManager,
  IDeviceStateLoggerRepository _deviceStateLoggerRepository) : AresDevices.AresDevicesBase
{
  public override Task<ListServerSerialPortsResponse> GetServerSerialPorts(Empty request, ServerCallContext? context)
  {
    var availableSerialPorts = SerialPort.GetPortNames();
    var cleanPorts = CleanSerialPorts(availableSerialPorts);
    var response = new ListServerSerialPortsResponse { SerialPorts = { cleanPorts } };
    return Task.FromResult(response);
  }

  private IEnumerable<string> CleanSerialPorts(IEnumerable<string> dirtyPortNames)
  {
    return dirtyPortNames.Select(s => s.IndexOf('\0') > 0 ? s[..s.IndexOf('\0')] : s);
  }

  public override async Task<Empty> Activate(DeviceActivateRequest request, ServerCallContext? context)
  {
    var device = GetAresDevice(request.DeviceId);
    if(device.Status.OperationalState == OperationalState.Active)
      return new Empty();

    await device.Activate(CancellationToken.None);
    return new Empty();
  }

  public override Task<ListAresDevicesResponse> ListAresDevices(Empty _, ServerCallContext? context)
  {
    var aresDeviceMessages = deviceProvider
      .GetAllDevices()
      .Select(GetInfo);

    var response = new ListAresDevicesResponse
    {
      AresDevices = { aresDeviceMessages }
    };

    return Task.FromResult(response);
  }

  public override Task<DeviceOperationalStatus> GetDeviceStatus(DeviceStatusRequest request, ServerCallContext? context)
  {
    try
    {
      var aresDevice = GetAresDevice(request.DeviceId);

      return Task.FromResult(aresDevice.Status);
    }
    catch(InvalidOperationException e)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = e.Message });
    }
  }

  public override Task<CommandMetadatasResponse> GetCommandMetadatas(CommandMetadatasRequest request, ServerCallContext? context)
  {
    var device = deviceProvider.GetDevice(request.DeviceId);

    var response = new CommandMetadatasResponse();

    if(device is null)
      return Task.FromResult(response);

    var metadata = CommandHelpers.ToCommandMetadata(device.CommandDescriptors, device.UniqueId);

    response.Metadatas.AddRange(metadata);

    return Task.FromResult(response);
  }

  public override async Task<DeviceExecutionResult> ExecuteCommand(CommandTemplate request, ServerCallContext? context)
  {
    var device = deviceProvider.GetDevice(request.UniqueId);
    var token = context?.CancellationToken ?? CancellationToken.None;

    if(device is null)
      return new DeviceExecutionResult() { Error = $"Couldn't find a device with the id {request.UniqueId}, cannot execute command!", Success = false };

    try
    {
      var arguments = new List<DeviceCommandArgument>();
      arguments.AddRange(request.Parameters.Select(p => new DeviceCommandArgument() { ArgName = p.Metadata.Name, ArgValue = p.Value }));

      Func<CancellationToken, Task<CommandResult>> internalAction = async (ct) 
        => await device.ExecuteCommand(request.Metadata.Name, arguments, ct);

      var result = await internalAction(token);

      return new DeviceExecutionResult()
      {
        Result = result.Result,
        Error = result.Error,
        Success = result.Success
      };
    }
    catch(Exception e)
    {
      return new DeviceExecutionResult() { Success = false, Error = e.ToString() };
    }
  }

  private IAresDevice GetAresDevice(string id)
  {
    var aresDevice = deviceProvider.GetDevice(id);

    if(aresDevice is null)
      throw new InvalidOperationException($"Could not find ARES device with id: {id}");

    return aresDevice;
  }

  public override async Task<DeviceConfigResponse> GetAllDeviceConfigs(DeviceConfigRequest request, ServerCallContext? context)
  {
    await using var dbContext = contextFactory.CreateDbContext();
    var configQuery = dbContext.DeviceConfigs.AsQueryable();
    if(!string.IsNullOrEmpty(request.DeviceType))
      configQuery = configQuery.Where(config => config.DriverName == request.DeviceType);

    var configs = await configQuery.ToArrayAsync();
    var response = new DeviceConfigResponse();
    response.Configs.AddRange(configs);
    return response;
  }

  public override Task<RemoteDeviceConfigResponse> GetAllRemoteDevicesConfigs(Empty request, ServerCallContext? context)
  {
    var remoteDevices = deviceProvider.GetAllDevices<RemoteDevice>().ToArray();

    var response = new RemoteDeviceConfigResponse();
    var configs = remoteDevices.Select(rd => new RemoteDeviceConfig { Name = rd.Name, UniqueId = rd.UniqueId, Url = rd.Address.ToString() });

    response.Configs.AddRange(configs);

    return Task.FromResult(response);
  }

  public override Task<ListAresRemoteDevicesResponse> ListRemoteAresDevices(Empty request, ServerCallContext? context)
  {
    var remoteDevices = deviceProvider.GetAllDevices<RemoteDevice>().ToArray();

    var response = new ListAresRemoteDevicesResponse();
    var infos = remoteDevices.Select(GetInfo);

    response.Devices.AddRange(infos);

    return Task.FromResult(response);
  }

  public override async Task<UpdateRemoteDeviceResponse> UpdateRemoteDevice(UpdateRemoteDeviceRequest request, ServerCallContext? context)
  {
    try
    {
      var deviceConfig = new RemoteDeviceConfig { UniqueId = request.DeviceId, Name = request.Name, Url = request.Url };
      await remoteDeviceManager.UpdateDevice(deviceConfig);
      var response = new UpdateRemoteDeviceResponse
      {
        Success = true
      };
      return response;
    }
    catch(Exception e)
    {
      var response = new UpdateRemoteDeviceResponse
      {
        Success = false,
        ErrorMessage = e.Message
      };
      return response;
    }
  }

  public override async Task<RemoveRemoteDeviceResponse> RemoveRemoteDevice(RemoveRemoteDeviceRequest request, ServerCallContext? context)
  {
    try
    {
      var removed = await remoteDeviceManager.RemoveDevice(request.DeviceId);
      return new RemoveRemoteDeviceResponse { Success = removed };
    }
    catch(Exception e)
    {
      return new RemoveRemoteDeviceResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override async Task<AddRemoteDeviceResponse> AddRemoteDevice(AddRemoteDeviceRequest request, ServerCallContext? context)
  {
    try
    {
      var device = await remoteDeviceManager.CreateDevice(request.Name, request.Url);
      return new AddRemoteDeviceResponse { Success = true, DeviceId = device.UniqueId };
    }
    catch(Exception e)
    {
      return new AddRemoteDeviceResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  public override Task<DeviceInfo> GetDeviceInfo(DeviceInfoRequest request, ServerCallContext? context)
  {
    var device = deviceProvider.GetDevice(request.DeviceId);
    if(device is null)
      return Task.FromResult(new DeviceInfo());

    var info = GetInfo(device);

    return Task.FromResult(info);
  }

  public override Task<AresStruct> GetDeviceSettings(DeviceSettingsRequest request, ServerCallContext? context)
  {
    var device = deviceProvider.GetDevice(request.DeviceId);
    if(device is not RemoteDevice remoteDevice)
    {
      return Task.FromResult(new AresStruct());
    }

    var aresSettings = new AresStruct();
    aresSettings.Fields.Add(remoteDevice.Settings);

    return Task.FromResult(aresSettings);
  }

  public override Task<Empty> SetDeviceSettings(DeviceSettings request, ServerCallContext? context)
  {
    var device = deviceProvider.GetDevice(request.DeviceId);
    if(device is not RemoteDevice remoteDevice)
    {
      return Task.FromResult(new Empty());
    }

    remoteDeviceManager.UpdateDeviceSettings(request);

    return Task.FromResult(new Empty());
  }

  public override Task<DeviceStateResponse> GetDeviceState(DeviceStateRequest request, ServerCallContext? context)
  {
    // We can do the non-remote devices later
    var device = deviceProvider.GetDevice<RemoteDevice>(request.DeviceId);
    if(device is null)
    {
      return Task.FromResult(new DeviceStateResponse());
    }

    var state = device.CurrentState;
    return Task.FromResult(state is null ? new DeviceStateResponse() : new DeviceStateResponse { State = state });
  }

  public override async Task GetDeviceStateStream(
      DeviceStateStreamRequest request,
      IServerStreamWriter<DeviceStateResponse> responseStream,
      ServerCallContext? context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var device = deviceProvider.GetDevice<RemoteDevice>(request.DeviceId);
    if(device is null || request.PollingSettings.PollingType == PollingType.None)
    {
      return;
    }

    var interval = request.PollingSettings.PollingType == PollingType.Interval && request.PollingSettings.IntervalMs == 0 ? 1000 : request.PollingSettings.IntervalMs;

    try
    {
      IObservable<AresStruct?> stateStream = device.StateStream.DistinctUntilChanged();
      if(request.PollingSettings.IntervalMs > 0)
      {
        stateStream = stateStream.Sample(TimeSpan.FromMilliseconds(interval));
      }

      await stateStream
        .ForEachAsync(async state =>
        {
          await responseStream.WriteAsync(new DeviceStateResponse { State = state }, cancellationToken);
        }, cancellationToken);
    }
    catch(OperationCanceledException)
    {
    }
  }

  public override Task<DeviceStateSchemaResponse> GetDeviceStateSchema(DeviceStateSchemaRequest request, ServerCallContext? context)
  {
    // We can do the non-remote devices later
    var device = deviceProvider.GetDevice<RemoteDevice>(request.DeviceId);
    if(device is null)
    {
      return Task.FromResult(new DeviceStateSchemaResponse { Schema = new AresStructSchema() });
    }

    var schema = device.StateSchema;
    return Task.FromResult(schema is null ? new DeviceStateSchemaResponse() : new DeviceStateSchemaResponse { Schema = schema });
  }

  public override async Task<Empty> SetDeviceLoggerSettings(DeviceLoggingSettings request, ServerCallContext? context)
  {
    await _stateLoggerManager.UpdateLogger(request.DeviceId, request);

    return new Empty();
  }

  public override Task<DeviceLoggersResponse> GetDeviceLoggers(Empty request, ServerCallContext? context)
  {
    var response = new DeviceLoggersResponse();
    var settingsResponses = _deviceStateLoggerRepository.Select(s => s.Value.Settings).ToArray();

    response.Loggers.AddRange(settingsResponses);

    return Task.FromResult(response);
  }

  public override Task<DeviceLoggingSettings> GetDeviceLoggerSettings(DeviceLoggerSettingsRequest request, ServerCallContext? context)
  {
    var settings = _stateLoggerManager.GetCurrentLoggerSettings(request.DeviceId);

    return Task.FromResult(settings);
  }

  public override Task<AvailableDeviceDriversResponse> GetAvailableDeviceDrivers(Empty request, ServerCallContext? context)
  {
    var availableDeviceDrivers = driverProvider.GetAllDeviceDrivers();

    var response = new AvailableDeviceDriversResponse();
    response.DeviceDrivers.AddRange(availableDeviceDrivers.Select(driver => driver.Manifest.DeviceTypeName));
    return Task.FromResult(response);
  }

  public override Task<DeviceConfigResponse> GetDeviceConfig(GetDeviceConfigRequest request, ServerCallContext? context)
  {
    //TODO: Maybe utilize the SHA's of the DLL's instead of names?
    var matchingDriver = driverProvider.GetDriverByName(request.DriverName);

    throw new NotImplementedException("IMPLEMENT THIS IN THE DEVICES SERVICE!");
  }

  public override Task<AvailableDevicesResponse> GetAllAvailableDevices(Empty request, ServerCallContext? context)
  {
    var response = new AvailableDevicesResponse();
    var devices = deviceProvider.GetAllDevices();

    response.Devices.AddRange(devices.Select(d => new AresDeviceDescription { DeviceName = d.Name, DeviceId = d.UniqueId }));
    return Task.FromResult(response);
  }

  public override async Task<AddDeviceResponse> AddAresDevice(AddDeviceRequest request, ServerCallContext? context)
  {
    try
    {
      var device = await deviceManager.Create(request.DeviceConfig);
      await deviceConfigManager.Add(device.UniqueId, device.Name, request.DeviceConfig);

      return new AddDeviceResponse() { Success = true };
    }

    catch(Exception ex)
    {
      return new AddDeviceResponse() { Success = false, ErrorMessage = $"Failed to add new device! Message: {ex.Message}"};
    }
  }

  private DeviceInfo GetInfo(IAresDevice device)
  {
    var info = new DeviceInfo
    {
      Name = device.Name,
      UniqueId = device.UniqueId,
      Description = device.Description,
      Type = device.Type,
      Version = device.Version,
    };

    if(device is RemoteDevice remoteDevice)
    {
      info.Url = remoteDevice.Address.ToString();
      info.SettingsSchema = remoteDevice.SettingSchema;
      info.Commands.AddRange(remoteDevice.CommandDescriptors);
    }

    else
      info.SettingsSchema = null;

    return info;
  }
}
