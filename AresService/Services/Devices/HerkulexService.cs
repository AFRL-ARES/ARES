using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HerkulexDRS;
using HerkulexDRS.Config;
using HerkulexDRS.Services;

namespace AresService.Services.Devices;


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
  private IServo GetServo(string id)
  {
    var servo = _deviceCommandInterpreterRepo
      .GetAresDevices<IServo>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(servo is null)
      throw new InvalidOperationException($"Could not find Servo: {id}");

    return servo;
  }

  public override async Task<Empty> PistonUp(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceId);
    await servo.PistonUp();
    return new Empty();
  }

  public override async Task<Empty> PistonDown(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceId);
    await servo.PistonDown();

    return new Empty();
  }

  public override async Task<Empty> ResetServo(DeviceRequest request, ServerCallContext context)
  {
    var servo = GetServo(request.DeviceId);
    await servo.ResetServo();

    return new Empty();
  }

  public override async Task<Empty> AddServo(ServoConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveServo(HerkulexRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.HerkulexId);
    await _configManager.Remove(request.HerkulexId);
    return new Empty();
  }

  public override Task<GetAllServosResponse> GetAllServos(Empty request, ServerCallContext context)
  {
    var deviceDescriptions = _deviceCommandInterpreterRepo.GetAresDevices<IServo>().Select(servoDood => new DeviceDescription { Id = servoDood.UniqueId, Name = servoDood.Name });
    var response = new GetAllServosResponse();
    response.Devices.AddRange(deviceDescriptions);
    return Task.FromResult(response);
  }

}
