using Ares.Core.Device;
using ARESCore.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using ValveController;
using ValveController.Config;
using ValveController.Services;

namespace ARESService.Services.Devices;

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

  private IValveController GetValveController(string name)
  {
    var valveController = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IValveController>()
      .FirstOrDefault(device => device.Name == name);

    if (valveController is null)
      throw new InvalidOperationException($"Could not find find Valve Controller :O {name}");

    return valveController;
  }

  public override Task<Empty> EngageRelayOne(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceName);
    valveController.EngageRelayOne();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> DisengageRelayOne(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceName);
    valveController.DisengageRelayOne();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> EngageRelayTwo(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceName);
    valveController.EngageRelayTwo();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> DisengageRelayTwo(DeviceRequest request, ServerCallContext context)
  {
    var valveController = GetValveController(request.DeviceName);
    valveController.DisengageRelayTwo();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> EnableRelays(DeviceRequest request, ServerCallContext context)
  {

    var valveController = GetValveController(request.DeviceName);
    valveController.EnableRelays();

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> RemoveValveController(ValveControllerRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceName);
    await _configManager.Remove(request.DeviceName);
    return new Empty();
  }

  public override async Task<Empty> AddValveController(ValveControllerConfig request, ServerCallContext context)
  {
    await _deviceManager.Load(request);
    await _configManager.Add(request.Name, request);
    return new Empty();
  }

  public override Task<GetAllValveControllersResponse> GetAllValveControllers(Empty request, ServerCallContext context)
  {
    var deviceNames = _deviceCommandInterpreterRepo.Select(deviceInterpreter => deviceInterpreter.Device).OfType<IValveController>().Select(servoDood => servoDood.Name);
    var response = new GetAllValveControllersResponse();
    response.DeviceNames.AddRange(deviceNames);
    return Task.FromResult(response);
  }

}
