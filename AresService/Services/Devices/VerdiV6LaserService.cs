using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.Repos;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using VerdiV6.Config;
using VerdiV6.Services;
using VerdiV6Laser;

namespace AresService.Services.Devices;

public class VerdiV6LaserService : VerdiV6Rpc.VerdiV6RpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<VerdiConfig, IVerdiV6Laser> _deviceManager;
  private readonly IDeviceConfigManager<VerdiConfig> _configManager;

  public VerdiV6LaserService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<VerdiConfig, IVerdiV6Laser> deviceManager,
    IDeviceConfigManager<VerdiConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private IVerdiV6Laser GetLaser(string id)
  {
    var laser = _deviceCommandInterpreterRepo
      .GetAresDevices<IVerdiV6Laser>()
      .FirstOrDefault(l => l.UniqueId == id);

    if(laser is null)
      throw new InvalidOperationException($"Could not find Laser: {id}");

    return laser;
  }

  public override async Task<Empty> SetLaserShutter(SetShutterRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    await laser.SetLaserShutter(request.Shutter);

    return new Empty();
  }

  public override async Task<GetShutterResponse> GetLaserShutter(DeviceRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    var shutter = await laser.GetLaserShutter();

    var response = new GetShutterResponse() { Shutter = shutter };
    return response;
  }

  public override async Task<GetLaserPowerResponse> GetLaserPower(DeviceRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    var power = await laser.GetLaserPower();

    var response = new GetLaserPowerResponse() { LaserPower = power };
    return response;
  }

  public override async Task<Empty> SetLaserPower(SetLaserPowerRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    await laser.SetLaserPower(request.LaserPower);

    return new Empty();
  }

  public override async Task<Empty> ActivateLaser(DeviceRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    await laser.ActivateLaser();

    return new Empty();
  }

  public override async Task<Empty> DeactivateLaser(DeviceRequest request, ServerCallContext context)
  {
    var laser = GetLaser(request.DeviceId);
    await laser.DeactivateLaser();

    return new Empty();
  }

  public override async Task<Empty> AddLaser(VerdiConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveLaser(DeviceRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateLaser(LaserUpdateRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }

  public override Task<GetAllLasersResponse> GetAllLasers(Empty request, ServerCallContext context)
  {
    var devices = _deviceCommandInterpreterRepo
      .GetAresDevices<IVerdiV6Laser>()
      .Select(laser => new DeviceDescription { Id = laser.UniqueId, Name = laser.Name });
    var response = new GetAllLasersResponse();
    response.Devices.AddRange(devices);
    return Task.FromResult(response);
  }
}
