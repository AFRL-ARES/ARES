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
using LindbergFurnace;
using TubeFurnace.Config;

namespace AresService.DeviceManagers;

public class TubeFurnaceManager : IDeviceManager<TubeFurnaceConfig, ITubeFurnace>
{
  private readonly ISerialConnectionManager<ITubeFurnaceConnection> _connectionManager;
  private readonly StateLoggerManager _stateLoggerManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public TubeFurnaceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<ITubeFurnaceConnection> connectionManager,
    StateLoggerManager stateLoggerManager,
    IDeviceStateLoggerRepository deviceStateLoggerRepo)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
    _stateLoggerManager = stateLoggerManager;
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
    await _stateLoggerManager.SetupLogger(device);
    var interpreter = new TubeFurnaceInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<ITubeFurnace> Update(string id, TubeFurnaceConfig config)
  {
    var existingFurnace = _deviceCommandInterpreterRepo
      .GetAresDevice<ITubeFurnace>(id);

    if(existingFurnace is null)
      return await Create(config);

    var currentAddress = await existingFurnace.GetCurrentAddress();
    // if nothing changed, don't bother re-adding the device
    if (existingFurnace.Connection.Name == config.PortName && currentAddress == config.Address)
      if((existingFurnace.Connection is SimTubeFurnaceConnection && config.Simulated) || (existingFurnace.Connection is TubeFurnaceConnection && !config.Simulated))
        return existingFurnace;

    await Remove(existingFurnace.UniqueId);

    return await Load(id, config);
  }

  public Task Remove(string tubeFurnaceId)
  {
    var tubeFurnace = _deviceCommandInterpreterRepo
      .GetAresDevice<ITubeFurnace>(tubeFurnaceId);

    if(tubeFurnace is null)
      return Task.CompletedTask;

    _deviceStateLoggerRepo.Remove(tubeFurnace.Name);
    tubeFurnace.Dispose();
    _deviceCommandInterpreterRepo.Remove(tubeFurnace.UniqueId);
    var connection = tubeFurnace.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<ITubeFurnaceConnection>>()
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
