using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.Remote;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Grpc.Services;

public class DevicesService : AresDevices.AresDevicesBase
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  readonly IRemoteDeviceManager _remoteDeviceManager;

  public DevicesService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, IDbContextFactory<CoreDatabaseContext> contextFactory, IRemoteDeviceManager remoteDeviceManager)
  {
    _remoteDeviceManager = remoteDeviceManager;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _dbContextFactory = contextFactory;
  }

  public override Task<ListServerSerialPortsResponse> GetServerSerialPorts(Empty request, ServerCallContext context)
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

  public override async Task<Empty> Activate(DeviceActivateRequest request, ServerCallContext context)
  {
    var device = GetAresDevice(request.DeviceId);
    if(device.Status.OperationalState == OperationalState.Active)
      return new Empty();

    await device.Activate();
    return new Empty();
  }

  public override Task<ListAresDevicesResponse> ListAresDevices(Empty _, ServerCallContext context)
  {
    var aresDeviceMessages = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .Select(device => new DeviceInfo() { Name = device.Name, Type = device.GetType().FullName });

    var response = new ListAresDevicesResponse
    {
      AresDevices = { aresDeviceMessages }
    };

    return Task.FromResult(response);
  }

  public override Task<DeviceOperationalStatus> GetDeviceStatus(DeviceStatusRequest request, ServerCallContext context)
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

  public override Task<CommandMetadatasResponse> GetCommandMetadatas(CommandMetadatasRequest request, ServerCallContext context)
  {
    var interpreter = _deviceCommandInterpreterRepo
      .First(commandInterpreter => commandInterpreter.Device.UniqueId == request.DeviceId);

    var commands = interpreter.CommandsToIndexedMetadatas();

    var response = new CommandMetadatasResponse();
    response.Metadatas.AddRange(commands);

    return Task.FromResult(response);
  }

  public override async Task<DeviceExecutionResult> ExecuteCommand(CommandTemplate request, ServerCallContext context)
  {
    var interpreter = _deviceCommandInterpreterRepo
      .First(commandInterpreter => commandInterpreter.Device.Name == request.Metadata.DeviceName);

    try
    {
      var deviceCommandTask = interpreter.TemplateToDeviceCommand(request);
      var result = await deviceCommandTask(context.CancellationToken);
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
    var aresDevice = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .FirstOrDefault(device => device.UniqueId == id);

    if(aresDevice is null)
      throw new InvalidOperationException($"Could not find ARES device with id: {id}");

    return aresDevice;
  }

  public override async Task<DeviceConfigResponse> GetAllDeviceConfigs(DeviceConfigRequest request, ServerCallContext context)
  {
    await using var dbContext = _dbContextFactory.CreateDbContext();
    var configQuery = dbContext.DeviceConfigs.AsQueryable();
    if(!string.IsNullOrEmpty(request.DeviceType))
      configQuery = configQuery.Where(config => config.DeviceType == request.DeviceType);

    var configs = await configQuery.ToArrayAsync();
    var response = new DeviceConfigResponse();
    response.Configs.AddRange(configs);
    return response;
  }

  public override Task<RemoteDeviceConfigResponse> GetAllRemoteDevicesConfigs(Empty request, ServerCallContext context)
  {
    var remoteDevices = _deviceCommandInterpreterRepo.Select(dci => dci.Device).OfType<RemoteDevice>().ToArray();

    var response = new RemoteDeviceConfigResponse();
    var configs = remoteDevices.Select(rd => new RemoteDeviceConfig { Name = rd.Name, UniqueId = rd.UniqueId, Url = rd.Address.ToString() });

    response.Configs.AddRange(configs);

    return Task.FromResult(response);
  }

  public override Task<ListAresRemoteDevicesResponse> ListRemoteAresDevices(Empty request, ServerCallContext context)
  {
    var remoteDevices = _deviceCommandInterpreterRepo.Select(dci => dci.Device).OfType<RemoteDevice>().ToArray();

    var response = new ListAresRemoteDevicesResponse();
    var infos = remoteDevices.Select(GetInfo);

    response.Devices.AddRange(infos);

    return Task.FromResult(response);
  }

  public override async Task<UpdateRemoteDeviceResponse> UpdateRemoteDevice(UpdateRemoteDeviceRequest request, ServerCallContext context)
  {
    try
    {
      var deviceConfig = new RemoteDeviceConfig { UniqueId = request.DeviceId, Name = request.Name, Url = request.Url };
      await _remoteDeviceManager.UpdateDevice(deviceConfig);
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

  public override async Task<RemoveRemoteDeviceResponse> RemoveRemoteDevice(RemoveRemoteDeviceRequest request, ServerCallContext context)
  {
    try
    {
      var removed = await _remoteDeviceManager.RemoveDevice(request.DeviceId);
      return new RemoveRemoteDeviceResponse { Success = removed };
    }
    catch(Exception e)
    {
      return new RemoveRemoteDeviceResponse { Success = false, ErrorMessage = e.Message };
    }
  }

  private DeviceInfo GetInfo(IAresDevice device)
  {
    return new DeviceInfo
    {
      Name = device.Name,
      UniqueId = device.UniqueId,
      Description = device.Description,
      Type = device.Type,
      Url = device is RemoteDevice remoteDevice ? remoteDevice.Address.ToString() : null,
      Version = device.Version
    };
  }
}
