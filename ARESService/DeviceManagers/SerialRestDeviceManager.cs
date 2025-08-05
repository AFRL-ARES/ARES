using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using RestSerialDevice;
using RestSerialDevice.Config;
using RestSerialDevice.Simulation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

  public async Task<ISerialRestDevice> Load(RestSerialConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    
    var device = new SerialRestDevice(config.Name, connection);
    await device.Init();
    await device.Activate();
    
    var interpreter = new SerialRestDeviceInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<ISerialRestDevice> Update(RestSerialConfig config)
  {
    var existingRestDevice = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialRestDevice>()
      .FirstOrDefault(device => device.Name == config.Name);

    if(existingRestDevice is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if(existingRestDevice.Connection.Name == config.PortName)
      if((existingRestDevice.Connection is SimRestSerialConnection && config.Simulated) || (existingRestDevice.Connection is SerialRestDeviceConnection && !config.Simulated))
        return existingRestDevice;

    await Remove(existingRestDevice.Name);

    return await Load(config);
  }

  public async Task Remove(string restDeviceName)
  {
    var restDeviceInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == restDeviceName);

    if(restDeviceInterpreter?.Device is not ISerialRestDevice restDevice)
      return;

    await restDevice.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(restDeviceInterpreter);
    var connection = restDevice.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<ISerialRestDeviceConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IEnumerable<ISerialRestDevice>> Load(IEnumerable<RestSerialConfig> configs)
  {
    var restDevice = await Task.WhenAll(configs.Select(Load));
    return restDevice;
  }


}
