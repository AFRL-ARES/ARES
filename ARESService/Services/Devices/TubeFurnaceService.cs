using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using LindbergFurnace;
using TubeFurnace.Config;
using TubeFurnace.Messaging;
using System.Reactive.Threading.Tasks;
using UnitsNet;

namespace AresService.Services.Devices;

public class TubeFurnaceService : TubeFurnaceRpc.TubeFurnaceRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<TubeFurnaceConfig, ITubeFurnace> _tubeFurnaceManager;
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceConfigManager<TubeFurnaceConfig> _configManager;

  public TubeFurnaceService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<TubeFurnaceConfig, ITubeFurnace> tubeFurnaceManager,
    IDbContextFactory<AresDbContext> dbContextFactory,
    IDeviceConfigManager<TubeFurnaceConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _tubeFurnaceManager = tubeFurnaceManager;
    _dbContextFactory = dbContextFactory;
    _configManager = configManager;
  }

  public override Task<GetAllTubeFurnacesResponse> GetAllTubeFurnaces(Empty request, ServerCallContext context)
  {

    var tubeFurnaceDescriptions = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ITubeFurnace>()
      .Select(device => device.StateStream.Take(1).ToTask().Result)
      .Select(OperationalState => new TubeFurnaceDeviceDescription { AssumedAddress = (int)OperationalState.AssumedAddress, Name = OperationalState.Name });

    var response = new GetAllTubeFurnacesResponse();
    response.TubeFurnaces.AddRange(tubeFurnaceDescriptions);
    return Task.FromResult(response);
  }

  public override async Task<Empty> GetSetpoint(TubeFurnaceRequest request, ServerCallContext context)
  {
    var device = GetTubeFurnace(request.TubeFurnaceName);
    await device.GetSetpoint();
    return new Empty();
  }
  public override async Task<Empty> SetSetpoint(SetSetpointRequest request, ServerCallContext context)
  {
    var device = GetTubeFurnace(request.DeviceRequest.TubeFurnaceName);
    var temperature = Temperature.FromDegreesCelsius(request.DegreesCelsius);
    await device.SetSetpoint(temperature);
    return new Empty();
  }

  public override async Task<Empty> GetCurrentTemperature(TubeFurnaceRequest request, ServerCallContext context)
  {
    var device = GetTubeFurnace(request.TubeFurnaceName);
    await device.GetCurrentTemperature();
    return new Empty();
  }

  public override async Task<Empty> UpdateTubeFurnace(TubeFurnaceConfig request, ServerCallContext context)
  {
    await _tubeFurnaceManager.Update(request);
    await _configManager.Update(request.Name, request);

    return new Empty();
  }

  public override async Task<TubeFurnaceState> GetState(TubeFurnaceRequest request, ServerCallContext context)
  {
    var tubeFurnace = GetTubeFurnace(request.TubeFurnaceName);
    var currentState = await tubeFurnace.StateStream.Take(1).ToTask();
    return currentState;
  }

  public override async Task<Empty> RemoveTubeFurnace(TubeFurnaceRequest request, ServerCallContext context)
  {
    await _tubeFurnaceManager.Remove(request.TubeFurnaceName);
    await _configManager.Remove(request.TubeFurnaceName);
    return new Empty();
  }

  private ITubeFurnace GetTubeFurnace(string name)
  {
    var tubeFurnace = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ITubeFurnace>()
      .FirstOrDefault(device => device.Name.Equals(name));

    if (tubeFurnace is null)
      throw new InvalidOperationException($"Could not find Tube Furnace: {name}");

    return tubeFurnace;
  }

  public override async Task<Empty> AddTubeFurnace(TubeFurnaceConfig request, ServerCallContext context)
  {
    await _tubeFurnaceManager.Load(request);
    var device = GetTubeFurnace(request.Name);
    request = await FillConfig(device, request);
    await _configManager.Add(request.Name, request);

    return new Empty();
  }


  /// <summary>
  /// Fills the config automatically based on the values we get directly from the step controller.
  /// That way the user does not have to manually look up the default/current values when
  /// first hooking up the controller
  /// </summary>
  /// <param name="controller"></param>
  /// <param name="config"></param>
  /// <returns></returns>
  private static Task<TubeFurnaceConfig> FillConfig(ITubeFurnace controller, TubeFurnaceConfig config)
  {
    //var state = await controller.StateStream.Where(s => s.Valid).Take(1);
    var newConfig = config.Clone();

    return Task.FromResult(newConfig);
  }
}