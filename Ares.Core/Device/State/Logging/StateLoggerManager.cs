using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.State.Logging;

public class StateLoggerManager(IDeviceStateLoggerRepository _stateLoggerRepository, IEnumerable<IDeviceStateLoggerFactory> _factories, ILogger<StateLoggerManager> _logger, IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  public async Task SetupLogger(IAresDevice device)
  {
    // Stop and remove any existing logger for the device
    if(_stateLoggerRepository.TryGetValue(device.UniqueId, out var existingLogger))
    {
      await existingLogger.Stop();
      _stateLoggerRepository.Remove(device.UniqueId);
    }

    var factory = _factories.FirstOrDefault(f => f.CanHandle(device));
    if(factory is null)
    {
      _logger.LogError("No suitable logger factory found for device type {DeviceType}", device.GetType().Name);
      return;
    }

    var logger = factory.Create(device);

    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == device.UniqueId);

    await logger.Start(existingSettings);
    _stateLoggerRepository.Add(device.UniqueId, logger);
  }

  public async Task RemoveLogger(string deviceId)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await logger.Stop();
      _stateLoggerRepository.Remove(deviceId);
    }

    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    if(existingSettings is not null)
    {
      ctx.DeviceLoggingSettings.Remove(existingSettings);
    }
  }

  public async Task UpdateLogger(string deviceId, DeviceLoggingSettings settings)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      await logger.UpdateSettings(settings);

      using var ctx = _dbContextFactory.CreateDbContext();
      var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
      if(existingSettings is not null)
      {
        existingSettings.IntervalMs = settings.IntervalMs;
        existingSettings.LoggingType = settings.LoggingType;
      }
      else
      {
        ctx.DeviceLoggingSettings.Add(settings);
      }

      await ctx.SaveChangesAsync();
    }
    else
    {
      throw new KeyNotFoundException($"No logger found for device {deviceId}");
    }
  }

  public DeviceLoggingSettings GetLoggerSettings(string deviceId)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      return logger.Settings;
    }
    else
    {
      throw new KeyNotFoundException($"No logger found for device {deviceId}");
    }
  }
}