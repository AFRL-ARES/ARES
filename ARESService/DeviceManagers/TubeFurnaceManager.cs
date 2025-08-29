using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using AresService.DeviceStateLoggers;
using AresService.DeviceStateLoggers.TubeFurnace;
using LindbergFurnace;
using TubeFurnace.Config;

namespace AresService.DeviceManagers;

public class TubeFurnaceManager : IDeviceManager<TubeFurnaceConfig, ITubeFurnace>
{
  private readonly ISerialConnectionManager<ITubeFurnaceConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  readonly IDeviceStateLoggerFactory<ITubeFurnace, ITubeFurnaceStateLogger> _stateLoggerFactory;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public TubeFurnaceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<ITubeFurnaceConnection> connectionManager,
    IDeviceStateLoggerFactory<ITubeFurnace, ITubeFurnaceStateLogger> stateLoggerFactory,
    IDeviceStateLoggerRepository deviceStateLoggerRepo)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _stateLoggerFactory = stateLoggerFactory;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<ITubeFurnace> Create(TubeFurnaceConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<ITubeFurnace> Load(string id, TubeFurnaceConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new LindbergFurnace.TubeFurnace(config.Name, config.Address, connection)
    {
      UniqueId = id
    };
    await device.Activate(CancellationToken.None);
    var logger = _stateLoggerFactory.Create(device);
    _deviceStateLoggerRepo[logger.DeviceId] = logger;
    await logger.Start();
    var interpreter = new TubeFurnaceInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<ITubeFurnace> Update(string id, TubeFurnaceConfig config)
  {
    var existingFurnace = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ITubeFurnace>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(existingFurnace is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingFurnace.Connection.Name == config.PortName)
      if((existingFurnace.Connection is SimTubeFurnaceConnection && config.Simulated) || (existingFurnace.Connection is TubeFurnaceConnection && !config.Simulated))
        return existingFurnace;

    await Remove(existingFurnace.UniqueId);

    return await Load(id, config);
  }

  public Task Remove(string tubeFurnaceId)
  {
    var tubeFurnaceInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.UniqueId == tubeFurnaceId);

    if(tubeFurnaceInterpreter?.Device is not ITubeFurnace tubeFurnace)
      return Task.CompletedTask;

    _deviceStateLoggerRepo.Remove(tubeFurnace.Name);
    tubeFurnace.Dispose();
    _deviceCommandInterpreterRepo.Remove(tubeFurnaceInterpreter);
    var connection = tubeFurnace.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<ITubeFurnaceConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);

    return Task.CompletedTask;
  }

  public async Task<ITubeFurnace[]> Load(IEnumerable<LoadableConfig<TubeFurnaceConfig>> configs)
  {
    var tubeFurnaces = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return tubeFurnaces;
  }
}
