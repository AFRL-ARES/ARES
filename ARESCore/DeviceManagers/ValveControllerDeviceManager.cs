using Ares.Core.Device;
using Ares.Device.Serial;
using ARESCore.ConnectionManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValveController;
using ValveController.Config;

namespace ARESCore.DeviceManagers;
public class ValveControllerDeviceManager : IDeviceManager<ValveControllerConfig, IValveController>
{
  private readonly IConnectionManager<IValveControllerConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public ValveControllerDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterepreterRepo, IConnectionManager<IValveControllerConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterepreterRepo;
    _connectionManager = connectionManager;
  }

  public async Task<IValveController> Load(ValveControllerConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new ValveController.ValveController(config.Name, connection);

    await device.Activate();
    var interpreter = new ValveControllerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IValveController> Update(ValveControllerConfig config)
  {
    var existingValveController = _deviceCommandInterpreterRepo
  .Select(interpreter => interpreter.Device)
  .OfType<IValveController>()
  .FirstOrDefault(device => device.Name == config.Name);

    if (existingValveController is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if (existingValveController.Connection.Name == config.PortName)
      if (existingValveController.Connection is SimValveControllerConnection && config.Simulated || existingValveController.Connection is ValveControllerConnection && !config.Simulated)
        return existingValveController;

    await Remove(existingValveController.Name);

    return await Load(config);
  }

  public async Task Remove(string valveControllerName)
  {
    var valveControllerInterpreter = _deviceCommandInterpreterRepo
  .FirstOrDefault(interpreter => interpreter.Device.Name == valveControllerName);

    if (valveControllerInterpreter?.Device is not IValveController valveController)
      return;

    await valveController.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(valveControllerInterpreter);
    var connection = valveController.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IValveControllerConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IEnumerable<IValveController>> Load(IEnumerable<ValveControllerConfig> configs)
  {
    var valveControllers = await Task.WhenAll(configs.Select(Load));
    return valveControllers;
  }
}
