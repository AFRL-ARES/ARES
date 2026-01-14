using Ares.Core.Device;
using Ares.Device;
using AresService.DeviceManagers;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.Services.Devices;

public class ChemyxPumpService : ChemyxPumpRpc.ChemyxPumpRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<ChemyxPumpConfig, IChemyxPump> _deviceManager;
  private readonly IDeviceConfigManager<ChemyxPumpConfig> _configManager;

  public ChemyxPumpService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, 
    IDeviceManager<ChemyxPumpConfig, IChemyxPump> deviceManager, 
    IDeviceConfigManager<ChemyxPumpConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private IChemyxPump GetPump(string id)
  {
    var pump = _deviceCommandInterpreterRepo
      .GetAresDevices<IChemyxPump>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(pump is null)
      throw new InvalidOperationException($"Could not find Pump: {id}");
  
    return pump;
  }

  public override Task<Empty> StartPump(StartPumpRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    if(pump is not null)
    {
      if(request.HasPumpNumber)
        pump.Start(request.PumpNumber);

      else
        pump.Start();
    }

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StopPump(StopPumpRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    if(pump is not null)
    {
      if(request.HasPumpNumber)
        pump.Stop(request.PumpNumber);

      else
        pump.Stop();
    }

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> PausePump(PausePumpRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    if(pump is not null)
    {
      if(request.HasPumpNumber)
        pump.Pause(request.PumpNumber);

      else
        pump.Pause();
    }

    return Task.FromResult(new Empty());
  }

  public override async Task<DispensedVolumeResponse> GetDispensedVolume(GetDispensedVolumeRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    var volumeDispensed = pump.GetDispensedVolume(request.PumpNumber);
    
    if(volumeDispensed is not null)
      return new DispensedVolumeResponse { VolumeDispense = (double)volumeDispensed };

    return new DispensedVolumeResponse { VolumeDispense = -1.0 };
  }

  public override async Task<ElapsedTimeResponse> GetElapsedTime(GetElapsedTimeRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    var elapsedTime = pump.GetElapsedTime(request.PumpNumber);

    if(elapsedTime is not null)
      return new ElapsedTimeResponse { ElapsedTime = elapsedTime.Value.ToDuration() };

    return new ElapsedTimeResponse { ElapsedTime = null };
  }

  public override async Task<LimitParameterResponse> GetLimitParameter(GetLimitParameterRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    var response = pump.ReadLimitParameter(request.PumpNumber);
    if(response is null)
      return new LimitParameterResponse();

    return new LimitParameterResponse { MaxRate = response.MaxRate, MaxVolume = response.MaxVolume, MinRate = response.MinRate, MinVolume = response.MinVolume };
  }

  public override async Task<PumpStatusResponse> GetPumpStatus(PumpStatusRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    var response = pump.GetStatus(request.PumpNumber);

    if(response is not null)
      return new PumpStatusResponse { PumpStatus = (int)response };

    return new PumpStatusResponse { PumpStatus = -1 }; 
  }

  public override Task<Empty> SetDelay(SetDelayRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    if(request.HasPumpNumber)
      pump.SetDelay(request.DesiredDelay.ToTimeSpan(), request.PumpNumber);

    else
      pump.SetDelay(request.DesiredDelay.ToTimeSpan());
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetDiameter(SetDiameterRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    if(request.HasPumpNumber)
      pump.SetDiameter(request.Diameter, request.PumpNumber);
    else
      pump.SetDiameter(request.Diameter);
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetPumpRate(SetPumpRateRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    if(request.HasPumpNumber)
      pump.SetRate(request.DesiredRate, request.PumpNumber);
    
    else
      pump.SetRate(request.DesiredRate);
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetTime(SetTimeRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    if(request.HasPumpNumber)
      pump.SetTime(request.DesiredTime.ToTimeSpan(), request.PumpNumber);

    else
      pump.SetTime(request.DesiredTime.ToTimeSpan());

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetUnits(SetUnitsRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    if(request.HasPumpNumber)
      pump.SetUnits(request.Unit.FromProto(), request.PumpNumber);

    else
      pump.SetUnits(request.Unit.FromProto());

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetVolume(SetVolumeRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);

    if(request.HasPumpNumber)
      pump.SetVolume(request.RequestedVolume, request.PumpNumber);

    else
      pump.SetVolume(request.RequestedVolume, request.PumpNumber);

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> AddChemyxPump(ChemyxPumpConfig request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveChemyxPump(PumpRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<ViewParametersResponse> GetViewParameters(GetViewParametersRequest request, ServerCallContext context)
  {
    var pump = GetPump(request.DeviceId);
    var parameters = pump.ViewParameters;
    var response = new ViewParametersResponse();
    if(parameters is null)
      return response;

    var pumpNum = 1;

    foreach(var pumpParams in parameters.PumpParameters)
    {
      var protoParams = new PumpParams
      {
        PumpNumber = pumpNum++,
        Unit = pumpParams.Units.ToProto(),
        Diameter = pumpParams.Diameter,
        Rate = pumpParams.Rate,
        Time = pumpParams.Time.ToDuration(),
        Volume = pumpParams.Volume,
        Delay = pumpParams.Delay.ToDuration()
      };

      response.Params.Add(protoParams);
    }

    return response;
  }

  public override Task<GetAllPumpsResponse> GetAllPumps(Empty request, ServerCallContext context)
  {
    var deviceDescriptions = _deviceCommandInterpreterRepo
      .GetAresDevices<IChemyxPump>()
      .Select(pump => new ChemyxPumpDeviceDescription { Id = pump.UniqueId, Name = pump.Name, DualPump = pump.DualPump });

    var response = new GetAllPumpsResponse();
    response.Devices.AddRange(deviceDescriptions);
    return Task.FromResult(response);
  }

  public override async Task<Empty> UpdatePump(UpdatePumpRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }
}
