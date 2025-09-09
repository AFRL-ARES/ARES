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
using AresService.DeviceStateLoggers.TicStepperController;
using Microsoft.Extensions.Logging;
using TicStepperController;
using TicStepperController.Config;

namespace AresService.DeviceManagers;
public class StepperControllerManager : IDeviceManager<StepperControllerConfig, IStepperController>
{
  readonly ISerialConnectionManager<IStepperControllerConnection> _connectionManager;
  readonly ILoggerFactory _loggerFactory;
  readonly IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger> _stateLoggerFactory;
  readonly IDeviceStateLoggerRepository _stateLoggerRepo;
  readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreters;

  public StepperControllerManager(
    ISerialConnectionManager<IStepperControllerConnection> connectionManager,
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

  public Task<IStepperController> Create(StepperControllerConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IStepperController> Load(string id, StepperControllerConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var ticLogger = _loggerFactory.CreateLogger<IStepperController>();
    var device = new StepperController(config.Name, connection, ticLogger)
    {
      UniqueId = id
    };
    var ticStateLogger = _stateLoggerFactory.Create(device);

    await device.Activate(CancellationToken.None);
    await device.Init(config);
    await device.Start();
    _stateLoggerRepo[device.Name] = ticStateLogger;
    await ticStateLogger.Start();

    var interpreter = new StepperControllerInterpreter(device);
    _deviceCommandInterpreters.Add(interpreter);
    return device;
  }

  public async Task<IStepperController[]> Load(IEnumerable<LoadableConfig<StepperControllerConfig>> configs)
  {
    return await Task.WhenAll(configs.Select(config => Load(config.Id, config.DeviceConfig)));
  }

  public async Task Remove(string deviceId)
  {
    var dataloggerInterpreter = _deviceCommandInterpreters
      .FirstOrDefault(interpreter => interpreter.Device.UniqueId == deviceId);

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

  public async Task<IStepperController> Update(string id, StepperControllerConfig config)
  {
    var device = _deviceCommandInterpreters
      .Select(dci => dci.Device)
      .OfType<IStepperController>()
      .FirstOrDefault(sc => sc.UniqueId == id);

    if (device is null)
      return await Load(id, config);

    if (ConnectionNeedsUpdating(device.Connection, config.Simulated, config.PortName))
    {
      await Remove(id);
      return await Load(id, config);
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
