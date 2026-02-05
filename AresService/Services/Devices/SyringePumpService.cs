using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.Repos;
using Ares.SyringePump.Ne1000.Messaging;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SyringePumpNE1000;
using UnitsNet;

namespace AresService.Services.Devices;

public class SyringePumpService : SyringePumpRpc.SyringePumpRpcBase
{
  private readonly IDeviceConfigManager<SyringePumpConfig> _configManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<SyringePumpConfig, ISyringePump> _deviceManager;

  public SyringePumpService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, IDeviceConfigManager<SyringePumpConfig> configManager, IDeviceManager<SyringePumpConfig, ISyringePump> deviceManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _configManager = configManager;
    _deviceManager = deviceManager;
  }

  public override Task<GetAllSyringePumpsResponse> GetAllSyringePumps(Empty request, ServerCallContext context)
  {
    var syringePumps = _deviceCommandInterpreterRepo
      .GetAresDevices<ISyringePump>()
      .Select(device => new SyringePumpDeviceDescription { AssumedAddress = (int)device.AssumedAddress, Name = device.Name, Id = device.UniqueId });

    var response = new GetAllSyringePumpsResponse();
    response.SyringePumps.AddRange(syringePumps);

    return Task.FromResult(response);
  }
  private ISyringePump? GetSyringePump(string id)
  {
    var syringePump = _deviceCommandInterpreterRepo
      .GetAresDevices<ISyringePump>()
      .FirstOrDefault(device => device.UniqueId == id);

    return syringePump;
  }

  public override Task<Empty> QueryPhaseFunction(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      syringePump.QueryPhaseFunction();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetPhase(SetPhaseNumberRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    if(syringePump is not null)
      await syringePump.SetPhase(request.Phase);
    return new Empty();
  }

  public override async Task<Empty> SetPhaseFunction(SetPhaseFunctionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    if(syringePump is not null)
      await syringePump.SetPhaseFunction(request.Function);
    return new Empty();
  }

  public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is null)
      return new ConnectResponse();

    var activationResult = await syringePump.Activate();
    var connectResponse = new ConnectResponse { Connected = activationResult };
    return connectResponse;
  }

  public override async Task<Empty> QueryPhase(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.QueryPhase();

    return new Empty();
  }

  public override Task<Empty> Disconnect(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    // syringePump.Disconnect();
    return Task.FromResult(new Empty());
  }

  public override async Task<StateResponse> GetCurrentState(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is null)
      return new StateResponse();

    var currentState = await syringePump.GetCurrentState();
    return currentState;
  }

  public override async Task<Empty> SetDiameter(SetDiameterMmRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var diameter = Length.FromMillimeters(request.DiameterMm);

    if(syringePump is not null)
      await syringePump.SetDiameter(diameter);
    return new Empty();
  }

  public override async Task<Empty> GetDiameter(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.GetDiameter();

    return new Empty();
  }

  public override async Task<Empty> SetProgramFunctionRate(SetProgramFunctionRateMmpmRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var pumpRate = Speed.FromMillimetersPerMinutes(request.RateMmpm);

    if(syringePump is not null)
      await syringePump.SetProgramFunctionRate(pumpRate);
    return new Empty();
  }

  public override async Task<Empty> GetProgramFunctionRate(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);

    if(syringePump is not null)
      await syringePump.GetProgramFunctionRate();
    return new Empty();
  }

  public override async Task<Empty> SetProgramFunctionVolumeToBeDispensed(SetProgramFunctionVolumeToBeDispensedRequest request,
    ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var volume = Volume.FromMilliliters(request.VolumeMl);

    if(syringePump is not null)
      await syringePump.SetProgramFunctionVolumeToBeDispensed(volume);
    return new Empty();
  }

  public override async Task<Empty> GetProgramFunctionVolumeToBeDispensed(DeviceRequest request,
    ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);

    if(syringePump is not null)
      await syringePump.GetProgramFunctionVolumeToBeDispensed();
    return new Empty();
  }

  public override async Task<Empty> SetProgramFunctionPumpingDirection(SetProgramFunctionPumpingDirectionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var direction = request.Direction;

    if(syringePump is not null)
      await syringePump.SetProgramFunctionPumpingDirection(direction);
    return new Empty();
  }

  public override async Task<Empty> GetProgramFunctionPumpingDirection(GetProgramFunctionPumpingDirectionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    if(syringePump is not null)
      await syringePump.GetProgramFunctionPumpingDirection();
    return new Empty();
  }

  public override async Task<Empty> StartPumpingProgram(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.StartPumpingProgram();
    return new Empty();
  }

  public override async Task<Empty> PurgePump(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.PurgePump();
    return new Empty();
  }

  public override async Task<Empty> StopPumpingProgram(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.StopPumpingProgram();
    return new Empty();
  }

  public override async Task<Empty> GetVolumeDispensed(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.GetVolumeDispensed();
    return new Empty();
  }

  public override async Task<Empty> ClearVolumeDispensed(ClearVolumeDispensedRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var direction = request.Direction;

    if(syringePump is not null)
      await syringePump.ClearVolumeDispensed(direction);
    return new Empty();
  }

  public override async Task<Empty> SetAddress(SetAddressRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceId);
    var address = request.Address;
    if(syringePump is not null)
      await syringePump.SetAddress(address);

    return new Empty();
  }

  public override async Task<Empty> GetAddress(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceId);
    if(syringePump is not null)
      await syringePump.GetAddress();
    return new Empty();
  }

  public override async Task<Empty> AddSyringePump(SyringePumpConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveSyringePump(SyringePumpRemoveRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateSyringePump(SyringePumpUpdateRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }
}
