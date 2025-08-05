using Ares.Core.Device;
using Ares.Device.Serial;
using Chiller.Config;
using AresService.ConnectionManagement;
using LaserChiller;
using LaserChiller.Simulated;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceManagers
{
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

    public async Task<ILaserChiller> Load(ChillerConfig config)
    {
      var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
      var device = new LaserChiller.LaserChiller(config.Name, connection);

      await device.Activate();
      var interpreter = new LaserChillerInterpreter(device);
      _deviceCommandInterpreterRepo.Add(interpreter);

      return device;
    }

    public async Task<IEnumerable<ILaserChiller>> Load(IEnumerable<ChillerConfig> configs)
    {
      var chillers = await Task.WhenAll(configs.Select(Load));
      return chillers;
    }

    public async Task Remove(string chillerName)
    {
      var chillerInterpreter = _deviceCommandInterpreterRepo
        .FirstOrDefault(interpreter => interpreter.Device.Name == chillerName);

      if(chillerInterpreter?.Device is not ILaserChiller chiller)
        return;

      await chiller.DisposeAsync();
      _deviceCommandInterpreterRepo.Remove(chillerInterpreter);
      var connection = chiller.Connection;
      var connectionInUse = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<ISerialDevice<ILaserChillerConnection>>()
        .Any(device => device.Connection == connection);

      if(!connectionInUse)
        _connectionManager.RemoveConnection(connection);
    }

    public async Task<ILaserChiller> Update(ChillerConfig config)
    {
      var existingChiller = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<ILaserChiller>()
        .FirstOrDefault(device => device.Name == config.Name);

      if(existingChiller is null)
        return await Load(config);

      // if nothing changed, don't bother re-adding the device
      if(existingChiller.Connection.Name == config.PortName)
        if((existingChiller.Connection is SimLaserChiller && config.Simulated) || (existingChiller.Connection is LaserChillerConnection && !config.Simulated))
          return existingChiller;

      await Remove(existingChiller.Name);

      return await Load(config);
    }
  }
}
