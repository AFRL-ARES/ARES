using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using SyringePumpNE1000;
using SyringePumpNE1000.Simulation;

namespace AresService.DeviceManagers;

public class SyringePumpManager : IDeviceManager<SyringePumpConfig, ISyringePump>
{
  private readonly ISerialConnectionManager<ISyringePumpConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly StateLoggerManager _stateLoggerManager;

  public SyringePumpManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<ISyringePumpConnection> connectionManager,
    StateLoggerManager stateLoggerManager)
  {
    _stateLoggerManager = stateLoggerManager;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<ISyringePump> Create(SyringePumpConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<ISyringePump> Load(string id, SyringePumpConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new SyringePump(config.Name, config.Address, connection)
    {
      UniqueId = id
    };
    await device.Activate(CancellationToken.None);
    await device.Start();
    await _stateLoggerManager.SetupLogger(device);
    var interpreter = new SyringePumpInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<ISyringePump> Update(string id, SyringePumpConfig config)
  {
    var existingPump = _deviceCommandInterpreterRepo
      .GetAresDevice<ISyringePump>(id);

    if(existingPump is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingPump.Connection.Name == config.PortName)
      if((existingPump.Connection is SimSyringePumpConnection && config.Simulated) || (existingPump.Connection is SyringePumpConnection && !config.Simulated))
        return existingPump;

    await Remove(existingPump.UniqueId);

    return await Load(id, config);
  }

  public async Task Remove(string syringePumpId)
  {
    var syringePump = _deviceCommandInterpreterRepo
      .GetAresDevice<ISyringePump>(syringePumpId);

    if(syringePump is null)
      return;

    await _stateLoggerManager.RemoveLogger(syringePump.UniqueId);
    await syringePump.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(syringePump.UniqueId);
    var connection = syringePump.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<ISyringePumpConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<ISyringePump[]> Load(IEnumerable<LoadableConfig<SyringePumpConfig>> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return pumps;
  }
}
