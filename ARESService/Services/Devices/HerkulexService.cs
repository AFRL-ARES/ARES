using Ares.Core.Device;
using ARESCore.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HerkulexDRS;
using HerkulexDRS.Config;
using HerkulexDRS.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ARESService.Services.Devices;

public class HerkulexService : HerkulexDRSRpc.HerkulexDRSRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<ServoConfig, IServo> _deviceManager;
  private readonly IDeviceConfigManager<ServoConfig> _configManager;

  public HerkulexService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, IDeviceManager<ServoConfig, IServo> deviceManager, IDeviceConfigManager<ServoConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }
  private IServo GetServo(string name)
  {
    var servo = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IServo>()
      .FirstOrDefault(device => device.Name == name);

    if (servo is null)
      throw new InvalidOperationException($"Could not find Servo >:(: {name}");

    return servo;
  }

  public override Task<Empty> PistonUp(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceName);
    servo.PistonUp();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> PistonDown(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceName);
    servo.PistonDown();

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> ResetServo(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceName);
    servo.ResetServo();

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> AddServo(ServoConfig request, ServerCallContext context)
  {
    await _deviceManager.Load(request);
    await _configManager.Add(request.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveServo(HerkulexRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.HerkulexName);
    await _configManager.Remove(request.HerkulexName);
    return new Empty();
  }

  public override Task<GetAllServosResponse> GetAllServos(Empty request, ServerCallContext context)
  {
    var deviceNames = _deviceCommandInterpreterRepo.Select(deviceInterpreter => deviceInterpreter.Device).OfType<IServo>().Select(servoDood => servoDood.Name);
    var response = new GetAllServosResponse();
    response.DeviceNames.AddRange(deviceNames);
    return Task.FromResult(response);
  }

}
