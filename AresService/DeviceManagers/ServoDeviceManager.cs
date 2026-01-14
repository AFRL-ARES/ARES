using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using HerkulexDRS;
using HerkulexDRS.Config;
using HerkulexDRS.Simulation;

namespace AresService.DeviceManagers;
public class ServoDeviceManager : IDeviceManager<ServoConfig, IServo>
{
  private readonly ISerialConnectionManager<IServoConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public ServoDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<IServoConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<IServo> Create(ServoConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IServo> Load(string id, ServoConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new Servo(config.Name, connection)
    {
      UniqueId = id
    };

    await device.Activate(CancellationToken.None);
    var interpreter = new ServoInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IServo> Update(string deviceId, ServoConfig config)
  {
    var existingServo = _deviceCommandInterpreterRepo
      .GetAresDevice<IServo>(deviceId);

    if(existingServo is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingServo.Connection.Name == config.PortName)
      if((existingServo.Connection is SimServoConnection && config.Simulated) || (existingServo.Connection is ServoConnection && !config.Simulated))
        return existingServo;

    await Remove(existingServo.UniqueId);

    return await Load(deviceId, config);
  }

  public async Task Remove(string servoId)
  {
    var servo = _deviceCommandInterpreterRepo
      .GetAresDevice<IServo>(servoId);

    if(servo is null)
      return;

    await servo.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(servo.UniqueId);
    var connection = servo.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<IServoConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      await _connectionManager.RemoveConnection(connection);
  }

  public async Task<IServo[]> Load(IEnumerable<LoadableConfig<ServoConfig>> configs)
  {
    var servos = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return servos;
  }
}

