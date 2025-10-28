using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AlicatMFC;
using AlicatMFC.Commands.Requests;
using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Core.Device;
using AresService.Data;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using UnitsNet;

namespace AresService.Services.Devices;

public class MfcService : MfcRpc.MfcRpcBase
{
  private readonly IDeviceConfigManager<MfcConfig> _configManager;
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<MfcConfig, IMassFlowController> _mfcManager;

  public MfcService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<MfcConfig, IMassFlowController> mfcManager,
    IDbContextFactory<AresDbContext> dbContextFactory,
    IDeviceConfigManager<MfcConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _dbContextFactory = dbContextFactory;
    _configManager = configManager;
    _mfcManager = mfcManager;
  }

  public override async Task<SetpointSourceResponse> GetSetpointSource(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    var response = new SetpointSourceResponse();
    var source = await mfc.GetSetpointSource();
    response.Source = source;
    
    return response;
  }

  public override async Task<Empty> SetSetpointSource(SetSetpointSourceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.Id);
    await mfc.SetSetpointSource(request.Source);
    return new Empty();
  }

  private IMassFlowController GetMfc(string id)
  {
    var mfc = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IMassFlowController>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(mfc is null)
      throw new InvalidOperationException($"Could not find MFC: {id}");

    return mfc;
  }

  public override async Task<Empty> SetSetpoint(SetSetpointRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceRequest.DeviceId);
    // TODO: Verify units
    await mfc.NewSetpoint(StandardVolumeFlow.FromStandardLitersPerMinute(request.Setpoint));
    return new Empty();
  }

  public override async Task<StateResponse> GetState(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    var latestState = await mfc.StateStream.Take(1);
    return latestState.ToProto();
  }

  public override async Task<StateResponse> GetStateUpdate(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    try
    {
      var latestState = await mfc.StateStream.Skip(1).Take(1);
      return latestState.ToProto();
    }
    catch(InvalidOperationException)
    {
      // Gets thrown when the device is disposed, just return empty response as at this point the state
      // is essentially invalid
      return new StateResponse();
    }
  }

  public override async Task<Empty> ChangeHardwareUnitId(ChangeUnitIdRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceRequest.DeviceId);
    await mfc.ChangeHardwareUnitId(request.Id[0]);
    return new Empty();
  }

  public override Task<Empty> TareAbsolutePressureWithBarometer(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    mfc.TareAbsolutePressureWithBarometer();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> TareFlow(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    mfc.TareFlow();
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> HoldValvesAtCurrentPosition(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    await mfc.HoldValvesAtCurrentPosition();
    return new Empty();
  }

  public override async Task<Empty> HoldValvesClosed(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    await mfc.HoldValvesClosed();
    return new Empty();
  }

  public override async Task<Empty> CancelValveHold(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    await mfc.CancelValveHold();
    return new Empty();
  }

  public override Task<Empty> NewComposerMix(ComposerMix request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceRequest.DeviceId);
    var entries = request.Entries.Select(entry => new MfcGasCompositionEntry(entry.GasNumber, entry.Percentage))
      .ToArray();

    var composerMix =
      new MfcGasComposition(request.Name, request.Number, entries);

    mfc.NewComposerMix(composerMix);
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> DeleteComposerMix(DeleteComposerMixRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceRequest.DeviceId);
    mfc.DeleteComposerMix(request.MixNumber);
    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> ChooseDifferentGas(ChooseDifferentGasRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceRequest.DeviceId);
    await mfc.ChooseDifferentGas(request.GasNumber);
    return new Empty();
  }

  public override Task<GetAllMfcsResponse> GetAllMfcs(Empty request, ServerCallContext context)
  {
    var mfcs = _deviceCommandInterpreterRepo.Select(interpreter => interpreter.Device)
      .OfType<IMassFlowController>()
      .Select(device => new MfcDeviceDescription { Id = device.UniqueId, Name = device.Name });

    var response = new GetAllMfcsResponse();
    response.Mfcs.AddRange(mfcs);

    return Task.FromResult(response);
  }

  public override async Task<GetAvailableIdsResponse> GetAvailableIds(GetAvailableIdsRequest request, ServerCallContext context)
  {
    // TODO consider maybe using the connections themselves to provide available ids?
    // ReSharper disable once MethodHasAsyncOverload
    await using var dbContext = _dbContextFactory.CreateDbContext();
    var mfcDeviceConfigs = await dbContext.DeviceConfigs
      .Where(config => config.DeviceType == typeof(IMassFlowController).FullName)
      .ToArrayAsync();

    var mfcConfigs = mfcDeviceConfigs.Select(config => config.ConfigData.Unpack<MfcConfig>())
      .Where(config => config.PortName.Equals(request.PortName, StringComparison.InvariantCultureIgnoreCase) && config.Simulated == request.Simulated);

    var usedIds = mfcConfigs.Select(config => config.Id.First());
    var availableIds = Enumerable.Range('A', 26).Select(i => (char)i).Except(usedIds);
    var response = new GetAvailableIdsResponse();
    response.Ids.AddRange(availableIds.Select(c => c.ToString()));
    return response;
  }

  public override async Task<Empty> RemoveMfc(MfcRemoveRequest request, ServerCallContext context)
  {
    await _mfcManager.Remove(request.MfcId);
    await _configManager.Remove(request.MfcId);
    return new Empty();
  }

  public override async Task<Empty> AddMfc(MfcConfig request, ServerCallContext context)
  {
    var mfc = await _mfcManager.Create(request);
    await mfc.Start();
    await _configManager.Add(mfc.UniqueId, mfc.Name, request);
    return new Empty();
  }

  public override async Task<Empty> UpdateMfc(MfcUpdateRequest request, ServerCallContext context)
  {
    var mfc = await _mfcManager.Update(request.Id, request.Config);
    await mfc.Start();
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }

  public override Task<Empty> StartDataCapture(DeviceRequest request, ServerCallContext context)
  {
    var mfc = GetMfc(request.DeviceId);
    mfc.Start();
    return Task.FromResult(new Empty());
  }
}
