using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.SyringePump.Ne1000.Messaging;
using ARESCore.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SyringePumpNE1000;
using UnitsNet;

namespace ARESService.Services.Devices;

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
    var syringePumps = _deviceCommandInterpreterRepo.Select(interpreter => interpreter.Device)
  .OfType<ISyringePump>()
  .Select(device => new SyringePumpDeviceDescription { AssumedAddress = (int)device.AssumedAddress, Name = device.Name });

    var response = new GetAllSyringePumpsResponse();
    response.SyringePumps.AddRange(syringePumps);

    return Task.FromResult(response);
  }
  private ISyringePump GetSyringePump(string name)
  {
    var syringePump = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISyringePump>()
      .FirstOrDefault(device => device.Name.Equals(name));

    if (syringePump is null)
      throw new InvalidOperationException($"Could not find Syringe Pump: {name}");

    return syringePump;
  }

  public override Task<Empty> QueryPhaseFunction(QueryPhaseFunctionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.QueryPhaseFunction();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetPhase(SetPhaseNumberRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    await syringePump.SetPhase(request.Phase);
    return new Empty();
  }

  public override async Task<Empty> SetPhaseFunction(SetPhaseFunctionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    await syringePump.SetPhaseFunction(request.Function);
    return new Empty();
  }

  public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
  {
    // var syringePumpConnection = new SyringePumpConnection("COM4");
    var syringePump = GetSyringePump(request.DeviceName);
    var activationResult = await syringePump.Activate();
    var connectResponse = new ConnectResponse { Connected = activationResult };
    return connectResponse;
  }

  public override Task<Empty> QueryPhase(QueryPhaseRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.QueryPhase();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> Disconnect(DeviceRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceName);
    // syringePump.Disconnect();
    return Task.FromResult(new Empty());
  }

  public override Task<StateResponse> GetCurrentState(GetCurrentStateRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var currentState = syringePump.GetCurrentState();
    return Task.FromResult(currentState);
  }

  public override Task<StateResponse> GetUpdatedState(GetCurrentStateRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var updatedState = syringePump.GetUpdatedState();
    return Task.FromResult(updatedState);
  }

  public override async Task<Empty> SetDiameter(SetDiameterMmRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var diameter = Length.FromMillimeters(request.DiameterMm);
    await syringePump.SetDiameter(diameter);
    return new Empty();
  }

  public override Task<Empty> GetDiameter(GetDiameterRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetDiameter();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetProgramFunctionRate(SetProgramFunctionRateMmpmRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var pumpRate = Speed.FromMillimetersPerMinutes(request.RateMmpm);
    await syringePump.SetProgramFunctionRate(pumpRate);
    return new Empty();
  }

  public override Task<Empty> GetProgramFunctionRate(GetProgramFunctionRateRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetProgramFunctionRate();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetProgramFunctionVolumeToBeDispensed(SetProgramFunctionVolumeToBeDispensedRequest request,
    ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var volume = Volume.FromMilliliters(request.VolumeMl);
    await syringePump.SetProgramFunctionVolumeToBeDispensed(volume);
    return new Empty();
  }

  public override Task<Empty> GetProgramFunctionVolumeToBeDispensed(GetProgramFunctionVolumeToBeDispensedRequest request,
    ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetProgramFunctionVolumeToBeDispensed();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetProgramFunctionPumpingDirection(SetProgramFunctionPumpingDirectionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var direction = request.Direction;
    await syringePump.SetProgramFunctionPumpingDirection(direction);
    return new Empty();
  }

  public override Task<Empty> GetProgramFunctionPumpingDirection(GetProgramFunctionPumpingDirectionRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetProgramFunctionPumpingDirection();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> StartPumpingProgram(StartPumpingProgramRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    await syringePump.StartPumpingProgram();
    return new Empty();
  }

  public override async Task<Empty> PurgePump(PurgePumpRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    await syringePump.PurgePump();
    return new Empty();
  }

  public override async Task<Empty> StopPumpingProgram(StopPumpingProgramRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    await syringePump.StopPumpingProgram();
    return new Empty();
  }

  public override Task<Empty> GetVolumeDispensed(GetVolumeDispensedRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetVolumeDispensed();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> ClearVolumeDispensed(ClearVolumeDispensedRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var direction = request.Direction;
    await syringePump.ClearVolumeDispensed(direction);
    return new Empty();
  }

  public override async Task<Empty> SetAddress(SetAddressRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    var address = request.Address;
    await syringePump.SetAddress(address);
    return new Empty();
  }

  public override Task<Empty> GetAddress(GetAddressRequest request, ServerCallContext context)
  {
    var syringePump = GetSyringePump(request.DeviceRequest.DeviceName);
    syringePump.GetAddress();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> AddSyringePump(SyringePumpConfig request, ServerCallContext context)
  {
    await _deviceManager.Load(request);
    await _configManager.Add(request.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveSyringePump(SyringePumpRemoveRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateSyringePump(SyringePumpConfig request, ServerCallContext context)
  {
    await _deviceManager.Update(request);
    await _configManager.Update(request.Name, request);
    return new Empty();
  }
}
