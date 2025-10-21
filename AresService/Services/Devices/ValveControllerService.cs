using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ValveController;
using ValveController.Config;
using ValveController.Services;

namespace AresService.Services.Devices;

public class ValveControllerService : ValveControllerRpc.ValveControllerRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<ValveControllerConfig, IValveController> _deviceManager;
  private readonly IDeviceConfigManager<ValveControllerConfig> _configManager;

  public ValveControllerService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<ValveControllerConfig,
    IValveController> deviceManager,
    IDeviceConfigManager<ValveControllerConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private IValveController GetValveController(string id)
  {
    var valveController = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IValveController>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(valveController is null)
      throw new InvalidOperationException($"Could not find find Valve Controller {id}");

    return valveController;
  }

  public override Task<Empty> EngageRelayOne(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceId);
    valveController.EngageRelayOne();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> DisengageRelayOne(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceId);
    valveController.DisengageRelayOne();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> EngageRelayTwo(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceId);
    valveController.EngageRelayTwo();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> DisengageRelayTwo(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceId);
    valveController.DisengageRelayTwo();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> EnableRelays(DeviceRequest request, ServerCallContext context)
  {

    var valveController = GetValveController(request.DeviceId);
    valveController.EnableRelays();

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> RemoveValveController(ValveControllerRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> AddValveController(ValveControllerConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override Task<GetAllValveControllersResponse> GetAllValveControllers(Empty request, ServerCallContext context)
  {
    var deviceDescriptions = _deviceCommandInterpreterRepo.Select(deviceInterpreter => deviceInterpreter.Device).OfType<IValveController>().Select(valveController => new DeviceDescription { Id = valveController.UniqueId, Name = valveController.Name });
    var response = new GetAllValveControllersResponse();
    response.Devices.AddRange(deviceDescriptions);
    return Task.FromResult(response);
  }

}
