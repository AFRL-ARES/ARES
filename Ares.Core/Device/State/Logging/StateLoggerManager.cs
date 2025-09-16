using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.State.Logging;

public class StateLoggerManager(IDeviceStateLoggerRepository _stateLoggerRepository, IEnumerable<IDeviceStateLoggerFactory> _factories, ILogger<StateLoggerManager> _logger, IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
{
  private readonly object _overrideSync = new();
  private bool _overrideActive;
  private int _overrideDepth;
  private DeviceLoggingSettings.Types.LoggingType _overrideLoggingType = DeviceLoggingSettings.Types.LoggingType.None;
  private readonly Dictionary<string, DeviceLoggingSettings> _originalSettings = new();
  private readonly Dictionary<string, DeviceLoggingSettings> _pendingSettings = new();

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

    if(TryGetOverrideType(out var overrideType))
    {
      await ApplyOverrideAsync(logger, overrideType);
    }
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

    lock(_overrideSync)
    {
      _originalSettings.Remove(deviceId);
      _pendingSettings.Remove(deviceId);
    }
  }

  public async Task UpdateLogger(string deviceId, DeviceLoggingSettings settings)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      var applyOverride = false;
      var overrideType = DeviceLoggingSettings.Types.LoggingType.None;

      lock(_overrideSync)
      {
        if(_overrideActive)
        {
          _pendingSettings[deviceId] = CloneSettings(settings, deviceId);
          applyOverride = true;
          overrideType = _overrideLoggingType;
        }
      }

      if(applyOverride)
      {
        await ApplyOverrideAsync(logger, overrideType);
      }
      else
      {
        await logger.UpdateSettings(settings);
      }

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
    if(TryGetOverrideSettings(deviceId, out var overrideSettings))
    {
      return overrideSettings;
    }

    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      return CloneSettings(logger.Settings, deviceId);
    }

    throw new KeyNotFoundException($"No logger found for device {deviceId}");
  }

  public async Task EnableOnChangeOverrideAsync()
  {
    await EnableOverrideAsync(DeviceLoggingSettings.Types.LoggingType.OnChange);
  }

  public async Task DisableOverrideAsync()
  {
    List<Task> restoreTasks = new();

    lock(_overrideSync)
    {
      if(!_overrideActive)
      {
        return;
      }

      _overrideDepth--;
      if(_overrideDepth > 0)
      {
        return;
      }

      foreach(var logger in _stateLoggerRepository.Values)
      {
        if(_pendingSettings.TryGetValue(logger.DeviceId, out var pending))
        {
          restoreTasks.Add(RestoreAsync(logger, pending));
        }
        else if(_originalSettings.TryGetValue(logger.DeviceId, out var original))
        {
          restoreTasks.Add(RestoreAsync(logger, original));
        }
      }

      _overrideActive = false;
      _overrideLoggingType = DeviceLoggingSettings.Types.LoggingType.None;
      _originalSettings.Clear();
      _pendingSettings.Clear();
    }

    await Task.WhenAll(restoreTasks);
  }

  private async Task EnableOverrideAsync(DeviceLoggingSettings.Types.LoggingType overrideType)
  {
    List<Task> overrideTasks = new();

    lock(_overrideSync)
    {
      if(_overrideActive)
      {
        _overrideDepth++;
        return;
      }

      _overrideActive = true;
      _overrideDepth = 1;
      _overrideLoggingType = overrideType;
    }

    foreach(var logger in _stateLoggerRepository.Values)
    {
      overrideTasks.Add(ApplyOverrideAsync(logger, overrideType));
    }

    await Task.WhenAll(overrideTasks);
  }

  private bool TryGetOverrideType(out DeviceLoggingSettings.Types.LoggingType loggingType)
  {
    lock(_overrideSync)
    {
      loggingType = _overrideLoggingType;
      return _overrideActive;
    }
  }

  private bool TryGetOverrideSettings(string deviceId, out DeviceLoggingSettings settings)
  {
    lock(_overrideSync)
    {
      if(_overrideActive)
      {
        if(_pendingSettings.TryGetValue(deviceId, out var pending))
        {
          settings = CloneSettings(pending, deviceId);
          return true;
        }

        if(_originalSettings.TryGetValue(deviceId, out var original))
        {
          settings = CloneSettings(original, deviceId);
          return true;
        }
      }
    }

    settings = default!;
    return false;
  }

  private async Task ApplyOverrideAsync(IDeviceStateLogger logger, DeviceLoggingSettings.Types.LoggingType overrideType)
  {
    DeviceLoggingSettings original;
    lock(_overrideSync)
    {
      if(!_originalSettings.TryGetValue(logger.DeviceId, out original!))
      {
        original = CloneSettings(logger.Settings, logger.DeviceId);
        _originalSettings[logger.DeviceId] = original;
      }
    }

    var overrideSettings = CloneSettings(original, logger.DeviceId);
    overrideSettings.LoggingType = overrideType;

    try
    {
      await logger.UpdateSettings(overrideSettings);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to apply logging override for device {DeviceId}", logger.DeviceId);
    }
  }

  private Task RestoreAsync(IDeviceStateLogger logger, DeviceLoggingSettings settings)
  {
    var restoreSettings = CloneSettings(settings, logger.DeviceId);

    return RestoreInternalAsync(logger, restoreSettings);
  }

  private async Task RestoreInternalAsync(IDeviceStateLogger logger, DeviceLoggingSettings settings)
  {
    try
    {
      await logger.UpdateSettings(settings);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to restore logging settings for device {DeviceId}", logger.DeviceId);
    }
  }

  private static DeviceLoggingSettings CloneSettings(DeviceLoggingSettings? settings, string deviceId)
  {
    if(settings is null)
    {
      return new DeviceLoggingSettings
      {
        DeviceId = deviceId,
        IntervalMs = 0,
        LoggingType = DeviceLoggingSettings.Types.LoggingType.None
      };
    }

    return new DeviceLoggingSettings
    {
      DeviceId = string.IsNullOrWhiteSpace(settings.DeviceId) ? deviceId : settings.DeviceId,
      IntervalMs = settings.IntervalMs,
      LoggingType = settings.LoggingType
    };
  }
}
