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

    await device.Activate(CancellationToken.None);
    var interpreter = new ChemyxPumpInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return device;
  }

  public async Task<IChemyxPump> Update(string deviceId, ChemyxPumpConfig config)
  {
    var existingPumps = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IChemyxPump>()
      .FirstOrDefault(device => device.UniqueId == deviceId);

    if(existingPumps is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingPumps.Connection.Name == config.PortName)
      if((existingPumps.Connection is SimChemyxPumpConnection && config.Simulated) || (existingPumps.Connection is ChemyxPumpConnection && !config.Simulated))
        return existingPumps;

    await Remove(existingPumps.UniqueId);

    return await Load(deviceId, config);
  }

  public async Task Remove(string pumpName)
  {
    var pumpInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == pumpName);

    if(pumpInterpreter?.Device is not IChemyxPump pump)
      return;

    await pump.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(pumpInterpreter);
    var connection = pump.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IChemyxPumpConnection>>()
      .Any(device => device.Connection == connection);

    if(!connectionInUse)
      _connectionManager.RemoveConnection(connection);
  }

  public async Task<IChemyxPump[]> Load(IEnumerable<LoadableConfig<ChemyxPumpConfig>> configs)
  {
    var pumps = await Task.WhenAll(configs.Select(c => Load(c.Id, c.DeviceConfig)));
    return pumps;
  }

}
