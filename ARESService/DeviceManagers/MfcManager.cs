using AlicatMFC;
using AlicatMFC.Simulation;
using Ares.Alicat.Mfc.Config;
using Ares.Core.Device;
using Ares.Device.Serial;
using AresService.ConnectionManagement;
using AresService.DeviceStateLoggers;
using AresService.DeviceStateLoggers.Mfc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceManagers;

public class MfcManager : IDeviceManager<MfcConfig, IMassFlowController>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly ISerialConnectionManager<IMfcConnection> _mfcConnectionManager;
  readonly IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger> _stateLoggerFactory;
  private readonly IList<IMassFlowController> _mfcs = new List<IMassFlowController>();
  readonly ILoggerFactory _loggerFactory;
  readonly IDeviceStateLoggerRepository _deviceStateLoggerRepo;

  public MfcManager(
    IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    ISerialConnectionManager<IMfcConnection> mfcConnectionManager,
    IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger> stateLoggerFactory,
    IDeviceStateLoggerRepository deviceStateLoggerRepo,
    ILoggerFactory loggerFactory)
  {
    _deviceStateLoggerRepo = deviceStateLoggerRepo;
    _loggerFactory = loggerFactory;
    _stateLoggerFactory = stateLoggerFactory;
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _mfcConnectionManager = mfcConnectionManager;
  }

  public async Task<IMassFlowController> Load(MfcConfig config)
  {
    var connection = _mfcConnectionManager.GetConnection(config.PortName, config.Simulated);
    var mfcLogger = _loggerFactory.CreateLogger<MassFlowController>();
    var device = new MassFlowController(config.Name, config.Id[0], connection, config.HasValve, mfcLogger);
    var mfcStateLogger = _stateLoggerFactory.Create(device);
    if (connection is SimMassFlowControllerConnection simConnection)
      simConnection.AddCat(config.Id[0]);

    await device.Activate();
    _deviceStateLoggerRepo[device.Name] = mfcStateLogger;
    await mfcStateLogger.Start();

    var interpreter = new MassFlowControllerInterpreter(device);
    _deviceCommandInterpreterRepo.Add(interpreter);
    return device;
  }

  public async Task<IEnumerable<IMassFlowController>> Load(IEnumerable<MfcConfig> configs)
  {
    var devices = new List<IMassFlowController>();
    foreach (var config in configs)
    {
      var device = await Load(config);
      devices.Add(device);
    }

    foreach (var device in devices)
    {
      if (device.Status.DeviceState == Ares.Messaging.Device.DeviceState.Active)
        await device.Start();
    }

    return devices;
  }

  public async Task<IMassFlowController> Update(MfcConfig config)
  {
    var existingMfc = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IMassFlowController>()
      .FirstOrDefault(device => device.Name == config.Name);

    if (existingMfc is null)
      return await Load(config);

    // if nothing changed, don't bother re-adding the device
    if (existingMfc.AssumedId == config.Id.First() && existingMfc.Connection.Name == config.PortName && existingMfc.HasValve == config.HasValve)
      if ((existingMfc.Connection is SimMassFlowControllerConnection && config.Simulated) || (existingMfc.Connection is MassFlowControllerConnection && !config.Simulated))
        return existingMfc;

    await Remove(existingMfc.Name);

    return await Load(config);
  }

  public async Task Remove(string mfcName)
  {
    var mfcInterpreter = _deviceCommandInterpreterRepo
      .FirstOrDefault(interpreter => interpreter.Device.Name == mfcName);

    if (mfcInterpreter?.Device is not IMassFlowController mfc)
      return;

    _mfcs.Remove(mfc);
    await mfc.DisposeAsync();
    _deviceStateLoggerRepo.Remove(mfc.Name);
    _deviceCommandInterpreterRepo.Remove(mfcInterpreter);
    var connection = mfc.Connection;
    var connectionInUse = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<ISerialDevice<IMfcConnection>>()
      .Any(device => device.Connection == connection);

    if (!connectionInUse)
      _mfcConnectionManager.RemoveConnection(connection);
  }
}
