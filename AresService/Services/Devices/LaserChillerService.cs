using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using Chiller.Config;
using Chiller.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LaserChiller;

namespace AresService.Services.Devices;

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

  private ILaserChiller GetChiller(string id)
  {
    var chiller = _deviceCommandInterpreterRepo
      .GetAresDevices<ILaserChiller>()
      .FirstOrDefault(d => d.UniqueId == id);

    if(chiller is null)
      throw new InvalidOperationException($"Could not find Laser Chiller: {id}");

    return chiller;
  }


  public override Task<ManifoldTemperatureResponse> GetManifoldTemperature(ChillerRequest request, ServerCallContext context)
  {
    var chiller = GetChiller(request.ChillerId);
    var temperature = chiller.InternalStateStream.Take(1).Wait();

    if(temperature is null)
      return Task.FromResult(new ManifoldTemperatureResponse());

    return Task.FromResult(new ManifoldTemperatureResponse() { ManifoldTemperature = temperature.Temperature });
  }

  public override async Task<Empty> SetChillerRunMode(ChillerRequest request, ServerCallContext context)
  {
    var chiller = GetChiller(request.ChillerId);
    await chiller.SetChillerRunMode();
    return new Empty();
  }

  public override async Task<Empty> SetChillerStandbyMode(ChillerRequest request, ServerCallContext context)
  {
    var chiller = GetChiller(request.ChillerId);
    await chiller.SetChillerStandbyMode();
    return new Empty();
  }

  public override async Task<Empty> SetChillerTemperature(SetChillerTemperatureRequest request, ServerCallContext context)
  {
    var chiller = GetChiller(request.ChillerId);
    await chiller.SetStabilizedTemperature(request.DesiredTemperature);
    return new Empty();
  }

  public override async Task<Empty> AddChiller(ChillerConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveChiller(ChillerRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.ChillerId);
    await _configManager.Remove(request.ChillerId);
    return new Empty();
  }

  public override async Task<Empty> UpdateChiller(UpdateChillerRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.ChillerId, request.Config);
    await _configManager.Update(request.ChillerId, request.Config);
    return new Empty();
  }

  public override Task<GetAllChillersResponse> GetAllChillers(Empty request, ServerCallContext context)
  {
    var chillers = _deviceCommandInterpreterRepo.GetAresDevices<ILaserChiller>().Select(chiller => new ChillerDescription { Id = chiller.UniqueId, Name = chiller.Name });
    var response = new GetAllChillersResponse();
    response.Chillers.AddRange(chillers);
    return Task.FromResult(response);
  }
}
