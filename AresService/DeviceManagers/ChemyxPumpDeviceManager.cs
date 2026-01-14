using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AresService.DeviceManagers;

public class ChemyxPumpDeviceManager : IDeviceManager<ChemyxPumpConfig, IChemyxPump>
{
  private readonly ISerialConnectionManager<IChemyxPumpConnection> _connectionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public ChemyxPumpDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, ISerialConnectionManager<IChemyxPumpConnection> connectionManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _connectionManager = connectionManager;
  }

  public Task<IChemyxPump> Create(ChemyxPumpConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IChemyxPump> Load(string id, ChemyxPumpConfig config)
  {
    var connection = _connectionManager.GetConnection(config.PortName, config.Simulated);
    var device = new ChemyxPump(config.Name, config.DualPump, connection)
    {
      UniqueId = id
    };

    var activationResult = await device.Activate(CancellationToken.None);
    if (activationResult)
      device.StartPolling();

    var interpreter = new ChemyxPumpInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IChemyxPump> Update(string deviceId, ChemyxPumpConfig config)
  {
    var existingPump = _deviceCommandInterpreterRepo
      .GetAresDevice<IChemyxPump>(deviceId);

    if(existingPump is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingPump.Connection.Name == config.PortName)
      if((existingPump.Connection is SimChemyxPumpConnection && config.Simulated) || (existingPump.Connection is ChemyxPumpConnection && !config.Simulated))
        return existingPump;

    await Remove(existingPump.UniqueId);

    return await Load(deviceId, config);
  }

  public async Task Remove(string pumpId)
  {
    var pump = _deviceCommandInterpreterRepo
      .GetAresDevice<ChemyxPump>(pumpId);

    if(pump is null)
      return;

    await pump.StopPolling();
    await pump.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(pump.UniqueId);
    var connection = pump.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .GetAresDevices<ISerialDevice<IChemyxPumpConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      await _connectionManager.RemoveConnection(connection);
  }

  public async Task<IChemyxPump[]> Load(IEnumerable<LoadableConfig<ChemyxPumpConfig>> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return pumps;
  }

}
