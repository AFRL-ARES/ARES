using Ares.Core.Device;
using Ares.Device.Serial;
using ARESCore.ConnectionManagement;
using HerkulexDRS;
using HerkulexDRS.Config;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARESCore.DeviceManagers;
public class ServoDeviceManager : IDeviceManager<ServoConfig, IServo>
{
  private readonly IConnectionManager<IServoConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;


  public ServoDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IConnectionManager<IServoConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public async Task<IServo> Load(ServoConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new Servo(config.Name, connection);

    await device.Activate();
    var interpreter = new ServoInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IServo> Update(ServoConfig config)
  {
    var existingServo = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IServo>()
      .FirstOrDefault(device => device.Name == config.Name);

    if (existingServo is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if (existingServo.Connection.Name == config.PortName)
      if (existingServo.Connection is SimServoConnection && config.Simulated || existingServo.Connection is ServoConnection && !config.Simulated)
        return existingServo;

    await Remove(existingServo.Name);

    return await Load(config);
  }

  public async Task Remove(string servoName)
  {
    var servoInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == servoName);

    if (servoInterpreter?.Device is not IServo servo)
      return;

    await servo.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(servoInterpreter);
    var connection = servo.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IServoConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IEnumerable<IServo>> Load(IEnumerable<ServoConfig> configs)
  {
    var servos = await Task.WhenAll(configs.Select(Load));
    return servos;
  }
}

