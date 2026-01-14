using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlicatMFC;
using AlicatMFC.Simulation;
using Ares.Alicat.Mfc.Config;
using Ares.Core.Device;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using Microsoft.Extensions.Logging;

namespace AresService.DeviceManagers;

public class MfcManager : IDeviceManager<MfcConfig, IMassFlowController>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly ISerialConnectionManager<IMfcConnection> _mfcConnectionManager;
  readonly ILoggerFactory _loggerFactory;
  private readonly StateLoggerManager _stateLoggerManager;

  public MfcManager(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<IMfcConnection> mfcConnectionManager,
    StateLoggerManager stateLoggerManager,
    ILoggerFactory loggerFactory)
  {
    _loggerFactory = loggerFactory;
    _stateLoggerManager = stateLoggerManager;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _mfcConnectionManager = mfcConnectionManager;
  }

  public Task<IMassFlowController> Create(MfcConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IMassFlowController> Load(string id, MfcConfig config)
  {
    var connection = _mfcConnectionManager.GetConnection(config.PortName, config.Simulated);
    var mfcLogger = _loggerFactory.CreateLogger<MassFlowController>();
    var device = new MassFlowController(config.Name, config.Id[0], connection, config.HasValve, config.MfcType, mfcLogger)
    {
      UniqueId = id
    };
    if(connection is SimMassFlowControllerConnection simConnection)
      simConnection.AddCat(config.Id[0], config.MfcType);

    await device.Activate(CancellationToken.None);
    await _stateLoggerManager.SetupLogger(device);

    var interpreter = new MassFlowControllerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<IMassFlowController[]> Load(IEnumerable<LoadableConfig<MfcConfig>> configs)
  {
    var devices = new List<IMassFlowController>();
    foreach (var config in configs)
    {
      var device = await Load(config.Id, config.DeviceConfig);
      devices.Add(device);
    }

    foreach(var device in devices)
    {
      if(device.Status.OperationalState == OperationalState.Active)
        await device.Start();
    }

    return devices.ToArray();
  }

  public async Task<IMassFlowController> Update(string id, MfcConfig config)
  {
    var existingMfc = _deviceCommandInterpreterRepo
      .GetAresDevice<IMassFlowController>(id);

    if(existingMfc is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingMfc.AssumedId == config.Id.First() && existingMfc.Connection.Name == config.PortName && existingMfc.HasValve == config.HasValve)
      if((existingMfc.Connection is SimMassFlowControllerConnection && config.Simulated) || (existingMfc.Connection is MassFlowControllerConnection && !config.Simulated))
        return existingMfc;

    await Remove(existingMfc.UniqueId);

    return await Load(id, config);
  }

  public async Task Remove(string mfcId)
  {
    var mfc = _deviceCommandInterpreterRepo
      .GetAresDevice<IMassFlowController>(mfcId);

    if(mfc is null)
      return;

    await mfc.DisposeAsync();
    await _stateLoggerManager.RemoveLogger(mfc.UniqueId);
    _deviceCommandInterpreterRepo.Remove(mfc.UniqueId);
    var connection = mfc.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IMfcConnection>>()
      .Any(device => device.Connection == connection);

    if(connection is SimMassFlowControllerConnection simCon)
    {
      simCon.RemoveCat(mfc.AssumedId);
    }

    if(!connectionInUse)
      _mfcConnectionManager.RemoveConnection(connection);
  }
}
