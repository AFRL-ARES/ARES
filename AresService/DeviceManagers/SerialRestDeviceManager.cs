using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using RestSerialDevice;
using RestSerialDevice.Config;
using RestSerialDevice.Simulation;

namespace AresService.DeviceManagers;

public class SerialRestDeviceManager : IDeviceManager<RestSerialConfig, ISerialRestDevice>
{
  private readonly ISerialConnectionManager<ISerialRestDeviceConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public SerialRestDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreters,
    ISerialConnectionManager<ISerialRestDeviceConnection> connectionManager)
  {
    _connectionManager = connectionManager;
    _deviceCommandInterpreterRepo = deviceCommandInterpreters;
  }

  public Task<ISerialRestDevice> Create(RestSerialConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<ISerialRestDevice> Load(string id, RestSerialConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);

    var device = new SerialRestDevice(config.Name, connection)
    {
      UniqueId = id
    };

    await device.Init();
    await device.Activate(CancellationToken.None);

    var interpreter = new SerialRestDeviceInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<ISerialRestDevice[]> Load(IEnumerable<LoadableConfig<RestSerialConfig>> configs)
  {
    var restDevices = await Task.WhenAll(configs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return restDevices;
  }


  public async Task<ISerialRestDevice> Update(string id, RestSerialConfig config)
  {
    var existingRestDevice = _deviceCommandInterpreterRepo
      .GetAresDevice<ISerialRestDevice>(id);

    if(existingRestDevice is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingRestDevice.Connection.Name == config.PortName)
      if((existingRestDevice.Connection is SimRestSerialConnection && config.Simulated) || (existingRestDevice.Connection is SerialRestDeviceConnection && !config.Simulated))
        return existingRestDevice;

    await Remove(existingRestDevice.UniqueId);

    return await Load(id, config);
  }

  public async Task Remove(string restDeviceId)
  {
    var restDevice = _deviceCommandInterpreterRepo
      .GetAresDevice<ISerialRestDevice>(restDeviceId);

    if(restDevice is null)
      return;

    await restDevice.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(restDevice.UniqueId);
    var connection = restDevice.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<ISerialRestDeviceConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }


}
