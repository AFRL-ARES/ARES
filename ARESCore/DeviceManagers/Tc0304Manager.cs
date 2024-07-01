using Ares.Core.Device;
using Ares.Device.Serial;
using ARESCore.ConnectionManagement;
using ARESCore.DeviceStateLoggers;
using ARESCore.DeviceStateLoggers.Tc0304;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tc0304.Config;
using TC0304;

namespace ARESCore.DeviceManagers;

public class Tc0304Manager : IDeviceManager<Tc0304Config, IDataloggerThermometer>
{
  private readonly IConnectionManager<IDataloggerThermometerConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  readonly IDeviceStateLoggerFactory<IDataloggerThermometer, ITc0304StateLogger> _stateLoggerFactory;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public Tc0304Manager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IConnectionManager<IDataloggerThermometerConnection> connectionManager,
    IDeviceStateLoggerRepository deviceStateLoggerRepo,
    IDeviceStateLoggerFactory<IDataloggerThermometer, ITc0304StateLogger> stateLoggerFactory)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _stateLoggerFactory = stateLoggerFactory;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public async Task<IDataloggerThermometer> Load(Tc0304Config config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new DataloggerThermometer(config.Name, connection);
    if (config.Probe1Name is not null)
      device.ProbeNames.T1Name = config.Probe1Name;

    if (config.Probe2Name is not null)
      device.ProbeNames.T2Name = config.Probe2Name;

    if (config.Probe3Name is not null)
      device.ProbeNames.T3Name = config.Probe3Name;

    if (config.Probe4Name is not null)
      device.ProbeNames.T4Name = config.Probe4Name;

    await device.Activate();
    var stateLogger = _stateLoggerFactory.Create(device);
    _deviceStateLoggerRepo[device.Name] = stateLogger;
    await stateLogger.Start();
    var interpreter = new DataLoggerThermometerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IDataloggerThermometer> Update(Tc0304Config config)
  {
    var existingLogger = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IDataloggerThermometer>()
      .FirstOrDefault(device => device.Name == config.Name);

    if (existingLogger is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if (existingLogger.Connection.Name == config.PortName)
      if (existingLogger.Connection is SimDataloggerThermometerConnection && config.Simulated || existingLogger.Connection is DataloggerThermometerConnection && !config.Simulated)
        return existingLogger;

    await Remove(existingLogger.Name);

    return await Load(config);
  }

  public async Task Remove(string dataloggerName)
  {
    var dataloggerInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == dataloggerName);

    if (dataloggerInterpreter?.Device is not IDataloggerThermometer logger)
      return;

    _deviceStateLoggerRepo.Remove(logger.Name);
    await logger.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(dataloggerInterpreter);
    var connection = logger.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IDataloggerThermometerConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IEnumerable<IDataloggerThermometer>> Load(IEnumerable<Tc0304Config> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(Load));
    return pumps;
  }
}
