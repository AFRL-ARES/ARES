using Ares.Core.Device;
using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using ARESCore.ConnectionManagement;
using ARESCore.DeviceStateLoggers;
using ARESCore.DeviceStateLoggers.SyringePump;
using SyringePumpNE1000;
using SyringePumpNE1000.Simulation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARESCore.DeviceManagers;

public class SyringePumpManager : IDeviceManager<SyringePumpConfig, ISyringePump>
{
  private readonly IConnectionManager<ISyringePumpConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  readonly IDeviceStateLoggerFactory<ISyringePump, ISyringePumpStateLogger> _stateLoggerFactory;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public SyringePumpManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IConnectionManager<ISyringePumpConnection> connectionManager,
    IDeviceStateLoggerFactory<ISyringePump, ISyringePumpStateLogger> stateLoggerFactory,
    IDeviceStateLoggerRepository deviceStateLoggerRepo)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _stateLoggerFactory = stateLoggerFactory;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public async Task<ISyringePump> Load(SyringePumpConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new SyringePump(config.Name, config.Address, connection);
    await device.Activate();
    var logger = _stateLoggerFactory.Create(device);
    _deviceStateLoggerRepo[logger.DeviceId] = logger;
    await logger.Start();
    var interpreter = new SyringePumpInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<ISyringePump> Update(SyringePumpConfig config)
  {
    var existingPump = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISyringePump>()
      .FirstOrDefault(device => device.Name == config.Name);

    if (existingPump is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if (existingPump.Connection.Name == config.PortName)
      if (existingPump.Connection is SimSyringePumpConnection && config.Simulated || existingPump.Connection is SyringePumpConnection && !config.Simulated)
        return existingPump;

    await Remove(existingPump.Name);

    return await Load(config);
  }

  public Task Remove(string syringePumpName)
  {
    var syringePumpInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == syringePumpName);

    if (syringePumpInterpreter?.Device is not ISyringePump syringePump)
      return Task.CompletedTask;

    _deviceStateLoggerRepo.Remove(syringePump.Name);
    syringePump.Dispose();
    _deviceCommandInterpreterRepo.Remove(syringePumpInterpreter);
    var connection = syringePump.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<ISyringePumpConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);

    return Task.CompletedTask;
  }

  public async Task<IEnumerable<ISyringePump>> Load(IEnumerable<SyringePumpConfig> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(Load));
    return pumps;
  }
}
