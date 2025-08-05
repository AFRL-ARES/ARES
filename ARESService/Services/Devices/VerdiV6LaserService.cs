using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerdiV6.Config;
using VerdiV6.Services;
using VerdiV6Laser;

namespace AresService.Services.Devices
{
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

    private IVerdiV6Laser GetLaser(string name)
    {
      var laser = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<IVerdiV6Laser>()
        .FirstOrDefault();

      if(laser is null)
        throw new InvalidOperationException($"Could not find Laser: {name}");

      return laser;
    }

    public override async Task<Empty> SetLaserShutter(SetShutterRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      await laser.SetLaserShutter(request.Shutter);

      return new Empty();
    }

    public override async Task<GetShutterResponse> GetLaserShutter(DeviceRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      var shutter = await laser.GetLaserShutter();

      var response = new GetShutterResponse() { Shutter = shutter };
      return response;
    }

    public override async Task<GetLaserPowerResponse> GetLaserPower(DeviceRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      var power = await laser.GetLaserPower();

      var response = new GetLaserPowerResponse() { LaserPower = power };
      return response;
    }

    public override async Task<Empty> SetLaserPower(SetLaserPowerRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      await laser.SetLaserPower(request.LaserPower);

      return new Empty();
    }

    public override async Task<Empty> ActivateLaser(DeviceRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      await laser.ActivateLaser();

      return new Empty();
    }

    public override async Task<Empty> DeactivateLaser(DeviceRequest request, ServerCallContext context)
    {
      var laser = GetLaser(request.DeviceName);
      await laser.DeactivateLaser();

      return new Empty();
    }

    public override async Task<Empty> AddLaser(VerdiConfig request, ServerCallContext context)
    {
      await _deviceManager.Load(request);
      await _configManager.Add(request.Name, request);
      return new Empty();
    }

    public override async Task<Empty> RemoveLaser(DeviceRequest request, ServerCallContext context)
    {
      await _deviceManager.Remove(request.DeviceName);
      await _configManager.Remove(request.DeviceName);
      return new Empty();
    }

    public override async Task<Empty> UpdateLaser(VerdiConfig request, ServerCallContext context)
    {
      await _deviceManager.Update(request);
      await _configManager.Update(request.Name, request);
      return new Empty();
    }

    public override Task<GetAllLasersResponse> GetAllLasers(Empty request, ServerCallContext context)
    {
      var deviceNames = _deviceCommandInterpreterRepo.Select(deviceInterpreter => deviceInterpreter.Device).OfType<IVerdiV6Laser>().Select(laser => laser.Name);
      var response = new GetAllLasersResponse();
      response.DeviceNames.AddRange(deviceNames);
      return Task.FromResult(response);
    }
  }
}
