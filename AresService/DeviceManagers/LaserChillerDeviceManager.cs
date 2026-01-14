using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using Chiller.Config;
using LaserChiller;
using LaserChiller.Simulated;

namespace AresService.DeviceManagers;

public class LaserChillerDeviceManager : IDeviceManager<ChillerConfig, ILaserChiller>
{
  private readonly ISerialConnectionManager<ILaserChillerConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public LaserChillerDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<ILaserChillerConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<ILaserChiller> Create(ChillerConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<ILaserChiller> Load(string id, ChillerConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new LaserChiller.LaserChiller(config.Name, connection)
    {
      UniqueId = id
    };

    await device.Activate(CancellationToken.None);
    var interpreter = new LaserChillerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<ILaserChiller[]> Load(IEnumerable<LoadableConfig<ChillerConfig>> configs)
  {
    var chillers = await Task.WhenAll(configs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return chillers;
  }

  public async Task Remove(string chillerId)
  {
    var chiller = _deviceCommandInterpreterRepo
      .GetAresDevice<ILaserChiller>(chillerId);

    if(chiller is null)
      return;

    await chiller.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(chiller.UniqueId);
    var connection = chiller.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<ILaserChillerConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<ILaserChiller> Update(string id, ChillerConfig config)
  {
    var existingChiller = _deviceCommandInterpreterRepo
      .GetAresDevice<ILaserChiller>(id);

    if(existingChiller is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingChiller.Connection.Name == config.PortName)
      if((existingChiller.Connection is SimLaserChiller && config.Simulated) || (existingChiller.Connection is LaserChillerConnection && !config.Simulated))
        return existingChiller;

    await Remove(existingChiller.UniqueId);

    return await Load(id, config);
  }
}
