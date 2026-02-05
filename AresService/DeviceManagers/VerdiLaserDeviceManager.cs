using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Core.Device.Repos;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
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

    public Task<IVerdiV6Laser> Create(VerdiConfig config)
    {
      return Load(Guid.NewGuid().ToString(), config);
    }

    public async Task<IVerdiV6Laser> Load(string id, VerdiConfig config)
    {
      var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
      var device = new VerdiV6Laser.VerdiV6Laser(config.Name, connection)
      {
        UniqueId = id
      };

      await device.Activate(CancellationToken.None);
      var interpreter = new VerdiV6LaserInterpreter(device);
      _deviceCommandInterpreterRepo.Add(interpreter);

      return device;
    }

    public async Task<IVerdiV6Laser> Update(string id, VerdiConfig config)
    {
      var existingLaser = _deviceCommandInterpreterRepo
        .GetAresDevice<IVerdiV6Laser>(id);

      if(existingLaser is null)
        return await Create(config);

      // if nothing changed, don't bother re-adding the device
      if(existingLaser.Connection.Name == config.PortName)
        if((existingLaser.Connection is SimulatedLaser && config.Simulated) || (existingLaser.Connection is LaserConnection && !config.Simulated))
          return existingLaser;

      await Remove(existingLaser.UniqueId);

      return await Load(id, config);
    }

    public async Task Remove(string laserId)
    {
      var laser = _deviceCommandInterpreterRepo
        .GetAresDevice<IVerdiV6Laser>(laserId);

      if(laser is null)
        return;

      await laser.DisposeAsync();
      _deviceCommandInterpreterRepo.Remove(laser.UniqueId);
      var connection = laser.Connection;
      var connectionInUse = _deviceCommandInterpreterRepo
        .GetAresDevices<ISerialDevice<ILaserConnection>>()
        .Any(device => device.Connection == connection);

      if(!connectionInUse)
        _connectionManager.RemoveConnection(connection);
    }

    public async Task<IVerdiV6Laser[]> Load(IEnumerable<LoadableConfig<VerdiConfig>> configs)
    {
      var lasers = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
      return lasers;
    }
  }
}
