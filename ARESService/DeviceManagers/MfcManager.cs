using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlicatMFC;
using AlicatMFC.Simulation;
using Ares.Alicat.Mfc.Config;
using Ares.Core.Device;
using Ares.Datamodel.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using AresService.DeviceStateLoggers;
using AresService.DeviceStateLoggers.Mfc;
using Microsoft.Extensions.Logging;

namespace AresService.DeviceManagers;

public class MfcManager : IDeviceManager<MfcConfig, IMassFlowController>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly ISerialConnectionManager<IMfcConnection> _mfcConnectionManager;
  readonly IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger> _stateLoggerFactory;
  readonly ILoggerFactory _loggerFactory;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public MfcManager(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<IMfcConnection> mfcConnectionManager,
    IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger> stateLoggerFactory,
    IDeviceStateLoggerRepository deviceStateLoggerRepo,
    ILoggerFactory loggerFactory)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _loggerFactory = loggerFactory;
    _stateLoggerFactory = stateLoggerFactory;
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
    var device = new MassFlowController(config.Name, config.Id[0], connection, config.HasValve, mfcLogger)
    {
      UniqueId = id
    };
    var mfcStateLogger = _stateLoggerFactory.Create(device);
    if(connection is SimMassFlowControllerConnection simConnection)
      simConnection.AddCat(config.Id[0]);

    await device.Activate(CancellationToken.None);
    _deviceStateLoggerRepo[device.UniqueId] = mfcStateLogger;
    await mfcStateLogger.Start();

    var interpreter = new MassFlowControllerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<IMassFlowController[]> Load(IEnumerable<LoadableConfig<MfcConfig>> configs)
  {
    var devices = await Task.WhenAll(configs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));

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
      .Select(interpreter => interpreter.Device)
      .OfType<IMassFlowController>()
      .FirstOrDefault(device => device.UniqueId == id);

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
    var mfcInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.UniqueId == mfcId);

    if(mfcInterpreter?.Device is not IMassFlowController mfc)
      return;

    await mfc.DisposeAsync();
    _deviceStateLoggerRepo.Remove(mfc.UniqueId);
    _deviceCommandInterpreterRepo.Remove(mfcInterpreter);
    var connection = mfc.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IMfcConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _mfcConnectionManager.RemoveConnection(connection);
  }
}
