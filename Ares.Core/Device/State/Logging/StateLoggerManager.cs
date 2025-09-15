using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.State.Logging;

public class StateLoggerManager(IDeviceStateLoggerRepository _stateLoggerRepository, IEnumerable<IDeviceStateLoggerFactory> _factories, ILogger<StateLoggerManager> _logger)
{
  public async Task SetupLogger(IAresDevice device)
  {
    // Stop and remove any existing logger for the device
    if (_stateLoggerRepository.TryGetValue(device.UniqueId, out var existingLogger))
    {
      await existingLogger.Stop();
      _stateLoggerRepository.Remove(device.UniqueId);
    }

    var factory = _factories.FirstOrDefault(f => f.CanHandle(device));
    if (factory is null)
    {
      _logger.LogError("No suitable logger factory found for device type {DeviceType}", device.GetType().Name);
      return;
    }

    var logger = factory.Create(device);
    await logger.Start();
    _stateLoggerRepository.Add(device.UniqueId, logger);
  }

  public async Task RemoveLogger(string deviceId)
  {
    if (_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await logger.Stop();
      _stateLoggerRepository.Remove(deviceId);
    }
  }

  public async Task UpdateLogger(string deviceId, DeviceLoggingSettings settings)
  {
    if (_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await logger.UpdateSettings(settings);
    }
    else
    {
      throw new KeyNotFoundException($"No logger found for device {deviceId}");
    }
  }
}