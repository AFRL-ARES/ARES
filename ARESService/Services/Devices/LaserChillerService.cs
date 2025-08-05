using Ares.Core.Device;
using Chiller.Config;
using Chiller.Services;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LaserChiller;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AresService.Services.Devices
{
  public class LaserChillerService : ChillerRpc.ChillerRpcBase
  {
    private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
    private readonly IDeviceManager<ChillerConfig, ILaserChiller> _deviceManager;
    private readonly IDeviceConfigManager<ChillerConfig> _configManager;

    public LaserChillerService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
      IDeviceManager<ChillerConfig, ILaserChiller> deviceManager,
      IDeviceConfigManager<ChillerConfig> configManager)
    {
      _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
      _deviceManager = deviceManager;
      _configManager = configManager;
    }

    private ILaserChiller GetChiller(string name)
    {
      var chiller = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<ILaserChiller>()
        .FirstOrDefault();

      if(chiller is null)
        throw new InvalidOperationException($"Could not find Laser Chiller: {name}");

      return chiller;
    }


    public override Task<ManifoldTemperatureResponse> GetManifoldTemperature(ChillerRequest request, ServerCallContext context)
    {
      var chiller = GetChiller(request.ChillerName);
      var temperature = chiller.StateStream.Take(1).Wait();

      if(temperature is null)
        return Task.FromResult(new ManifoldTemperatureResponse());

      return Task.FromResult(new ManifoldTemperatureResponse() { ManifoldTemperature = temperature.Temperature });
    }

    public override async Task<Empty> SetChillerRunMode(ChillerRequest request, ServerCallContext context)
    {
      var chiller = GetChiller(request.ChillerName);
      await chiller.SetChillerRunMode();
      return new Empty();
    }

    public override async Task<Empty> SetChillerStandbyMode(ChillerRequest request, ServerCallContext context)
    {
      var chiller = GetChiller(request.ChillerName);
      await chiller.SetChillerStandbyMode();
      return new Empty();
    }

    public override async Task<Empty> SetChillerTemperature(SetChillerTemperatureRequest request, ServerCallContext context)
    {
      var chiller = GetChiller(request.ChillerName);
      await chiller.SetStabilizedTemperature(request.DesiredTemperature);
      return new Empty();
    }

    public override async Task<Empty> AddChiller(ChillerConfig request, ServerCallContext context)
    {
      await _deviceManager.Load(request);
      await _configManager.Add(request.Name, request);
      return new Empty();
    }

    public override async Task<Empty> RemoveChiller(ChillerRequest request, ServerCallContext context)
    {
      await _deviceManager.Remove(request.ChillerName);
      await _configManager.Remove(request.ChillerName);
      return new Empty();
    }

    public override async Task<Empty> UpdateChiller(ChillerConfig request, ServerCallContext context)
    {
      await _deviceManager.Update(request);
      await _configManager.Update(request.Name, request);
      return new Empty();
    }

    public override Task<GetAllChillersResponse> GetAllChillers(Empty request, ServerCallContext context)
    {
      var deviceNames = _deviceCommandInterpreterRepo.Select(deviceInterpreter => deviceInterpreter.Device).OfType<ILaserChiller>().Select(chiller => chiller.Name);
      var response = new GetAllChillersResponse();
      response.DeviceNames.AddRange(deviceNames);
      return Task.FromResult(response);
    }
  }
}
