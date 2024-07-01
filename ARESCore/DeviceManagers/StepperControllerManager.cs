using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using ARESCore.ConnectionManagement;
using ARESCore.DeviceStateLoggers;
using ARESCore.DeviceStateLoggers.TicStepperController;
using Microsoft.Extensions.Logging;
using TicStepperController;
using TicStepperController.Config;

namespace ARESCore.DeviceManagers;
public class StepperControllerManager : IDeviceManager<StepperControllerConfig, IStepperController>
{
  readonly IConnectionManager<IStepperControllerConnection> _connectionManager;
  readonly ILoggerFactory _loggerFactory;
  readonly IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger> _stateLoggerFactory;
  readonly IDeviceStateLoggerRepository _stateLoggerRepo;
  readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreters;

  public StepperControllerManager(
    IConnectionManager<IStepperControllerConnection> connectionManager,
    ILoggerFactory loggerFactory,
    IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger> stateLoggerFactory,
    IDeviceStateLoggerRepository stateLoggerRepo,
    IDeviceCommandInterpreterRepo deviceCommandInterpreters)
  {
    _deviceCommandInterpreters = deviceCommandInterpreters;
    _stateLoggerRepo = stateLoggerRepo;
    _stateLoggerFactory = stateLoggerFactory;
    _loggerFactory = loggerFactory;
    _connectionManager = connectionManager;
  }

  public async Task<IStepperController> Load(StepperControllerConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var ticLogger = _loggerFactory.CreateLogger<IStepperController>();
    var device = new StepperController(config.Name, connection, ticLogger);
    var ticStateLogger = _stateLoggerFactory.Create(device);

    await device.Activate();
    await device.Init(config);
    await device.Start();
    _stateLoggerRepo[device.Name] = ticStateLogger;
    await ticStateLogger.Start();

    var interpreter = new StepperControllerInterpreter(device);
    _deviceCommandInterpreters.Add(interpreter);
    return device;
  }

  public async Task<IEnumerable<IStepperController>> Load(IEnumerable<StepperControllerConfig> configs)
  {
    return await Task.WhenAll(configs.Select(config => Load(config)));
  }

  public async Task Remove(string deviceId)
  {
    var dataloggerInterpreter = _deviceCommandInterpreters
      .FirstOrDefault(interpreter => interpreter.Device.Name == deviceId);

    if (dataloggerInterpreter?.Device is not IStepperController controller)
      return;

    _stateLoggerRepo.Remove(controller.Name);
    await controller.DisposeAsync();
    _deviceCommandInterpreters.Remove(dataloggerInterpreter);
    var connection = controller.Connection;
    var connectionInUse = _deviceCommandInterpreters
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IStepperControllerConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IStepperController> Update(StepperControllerConfig config)
  {
    var device = _deviceCommandInterpreters
      .Select(dci => dci.Device)
      .OfType<IStepperController>()
      .FirstOrDefault(sc => sc.Name == config.Name);

    if (device is null)
      return await Load(config);

    if (ConnectionNeedsUpdating(device.Connection, config.Simulated, config.PortName))
    {
      await Remove(config.Name);
      return await Load(config);
    }

    await device.Init(config);
    return device;
  }

  private static bool ConnectionNeedsUpdating(IStepperControllerConnection connection, bool simulated, string portName)
  {
    if (connection is SimStepperControllerConnection && !simulated)
      return true;
    if (connection is StepperControllerConnection && simulated)
      return true;
    if (connection.Name != portName)
      return true;

    return false;
  }
}
