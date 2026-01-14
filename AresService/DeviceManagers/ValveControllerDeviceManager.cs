using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using ValveController;
using ValveController.Config;
using ValveController.Simulated;

namespace AresService.DeviceManagers;
public class ValveControllerDeviceManager : IDeviceManager<ValveControllerConfig, IValveController>
{
  private readonly ISerialConnectionManager<IValveControllerConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public ValveControllerDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterepreterRepo, ISerialConnectionManager<IValveControllerConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterepreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<IValveController> Create(ValveControllerConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IValveController> Load(string id, ValveControllerConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new ValveController.ValveController(config.Name, connection)
    {
      UniqueId = id
    };

    await device.Activate(CancellationToken.None);
    var interpreter = new ValveControllerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IValveController> Update(string id, ValveControllerConfig config)
  {
    var existingValveController = _deviceCommandInterpreterRepo
      .GetAresDevice<IValveController>(id);

    if(existingValveController is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingValveController.Connection.Name == config.PortName)
      if((existingValveController.Connection is SimValveControllerConnection && config.Simulated) || (existingValveController.Connection is ValveControllerConnection && !config.Simulated))
        return existingValveController;

    await Remove(existingValveController.UniqueId);

    return await Load(id, config);
  }

  public async Task Remove(string valveControllerId)
  {
    var valveController = _deviceCommandInterpreterRepo
      .GetAresDevice<IValveController>(valveControllerId);

    if(valveController is null)
      return;

    await valveController.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(valveController.UniqueId);
    var connection = valveController.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<IValveControllerConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IValveController[]> Load(IEnumerable<LoadableConfig<ValveControllerConfig>> configs)
  {
    var valveControllers = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return valveControllers;
  }
}
