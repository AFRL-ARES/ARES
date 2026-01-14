using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.State.Logging;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using TC0304;
using Tc0304.Config;

namespace AresService.DeviceManagers;

public class Tc0304Manager : IDeviceManager<Tc0304Config, IDataloggerThermometer>
{
  private readonly ISerialConnectionManager<IDataloggerThermometerConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly StateLoggerManager _stateLoggerManager;

  public Tc0304Manager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<IDataloggerThermometerConnection> connectionManager,
    StateLoggerManager stateLoggerManager)
  {
    _stateLoggerManager = stateLoggerManager;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<IDataloggerThermometer> Create(Tc0304Config config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IDataloggerThermometer> Load(string id, Tc0304Config config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new DataloggerThermometer(config.Name, connection)
    {
      UniqueId = id
    };

    if (config.Probe1Name is not null)
      device.ProbeNames.T1Name = config.Probe1Name;

    if (config.Probe2Name is not null)
      device.ProbeNames.T2Name = config.Probe2Name;

    if (config.Probe3Name is not null)
      device.ProbeNames.T3Name = config.Probe3Name;

    if (config.Probe4Name is not null)
      device.ProbeNames.T4Name = config.Probe4Name;

    await device.Activate(CancellationToken.None);
    await _stateLoggerManager.SetupLogger(device);
    var interpreter = new DataLoggerThermometerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IDataloggerThermometer> Update(string id, Tc0304Config config)
  {
    var existingLogger = _deviceCommandInterpreterRepo
      .GetAresDevice<IDataloggerThermometer>(id);

    if (existingLogger is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if (existingLogger.Connection.Name == config.PortName)
      if ((existingLogger.Connection is SimDataloggerThermometerConnection && config.Simulated) || (existingLogger.Connection is DataloggerThermometerConnection && !config.Simulated))
        return existingLogger;

    await Remove(existingLogger.UniqueId);

    return await Load(id, config);
  }

  public async Task Remove(string dataloggerId)
  {
    var logger = _deviceCommandInterpreterRepo
      .GetAresDevice<IDataloggerThermometer>(dataloggerId);

    if (logger is null)
      return;

    await _stateLoggerManager.RemoveLogger(logger.UniqueId);
    await logger.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(logger.UniqueId);
    var connection = logger.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<IDataloggerThermometerConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IDataloggerThermometer[]> Load(IEnumerable<LoadableConfig<Tc0304Config>> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return pumps;
  }
}
