using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerdiV6.Config;
using VerdiV6Laser;
using VerdiV6Laser.Simulated;

namespace AresService.DeviceManagers
{
  public class VerdiLaserDeviceManager : IDeviceManager<VerdiConfig, IVerdiV6Laser>
  {
    private readonly ISerialConnectionManager<ILaserConnection> _connectionManager;
    private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

    public VerdiLaserDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
      ISerialConnectionManager<ILaserConnection> connectionManager)
    {
      _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
      _connectionManager = connectionManager;
    }

    public async Task<IVerdiV6Laser> Load(VerdiConfig config)
    {
      var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
      var device = new VerdiV6Laser.VerdiV6Laser(config.Name, connection);

      await device.Activate();
      var interpreter = new VerdiV6LaserInterpreter(device);
      _deviceCommandInterpreterRepo.Add(interpreter);

      return device;
    }

    public async Task<IVerdiV6Laser> Update(VerdiConfig config)
    {
      var existingLaser = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<IVerdiV6Laser>()
        .FirstOrDefault(device => device.Name == config.Name);

      if(existingLaser is null)
        return await Load(config);

      // if nothing changed, don't bother re-adding the device
      if(existingLaser.Connection.Name == config.PortName)
        if((existingLaser.Connection is SimulatedLaser && config.Simulated) || (existingLaser.Connection is LaserConnection && !config.Simulated))
          return existingLaser;

      await Remove(existingLaser.Name);

      return await Load(config);
    }

    public async Task Remove(string laserName)
    {
      var laserInterpreter = _deviceCommandInterpreterRepo
        .FirstOrDefault(interpreter => interpreter.Device.Name == laserName);

      if(laserInterpreter?.Device is not IVerdiV6Laser laser)
        return;

      await laser.DisposeAsync();
      _deviceCommandInterpreterRepo.Remove(laserInterpreter);
      var connection = laser.Connection;
      var connectionInUse = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<ISerialDevice<ILaserConnection>>()
        .Any(device => device.Connection == connection);

      if(!connectionInUse)
        _connectionManager.RemoveConnection(connection);
    }

    public async Task<IEnumerable<IVerdiV6Laser>> Load(IEnumerable<VerdiConfig> configs)
    {
      var lasers = await Task.WhenAll(configs.Select(Load));
      return lasers;
    }
  }
}
