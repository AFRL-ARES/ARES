using Ares.Core.Device.Providers;
using Ares.Datamodel.Device;
using Ares.Device;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace Ares.Core.Device.State.Logging;

public class StateLoggerManager
{
  private readonly IDeviceStateLoggerRepository _stateLoggerRepository;
  private readonly IDeviceStateLoggerFactory _deviceLoggerFactory;
  private readonly ILogger<StateLoggerManager> _logger;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly CompositeDisposable _cleanup = new();
  private bool _overrideActive;
  private DeviceLoggingSettings? _overrideSettings;
  private readonly SemaphoreSlim _overrideLock = new(1, 1);

  public StateLoggerManager(IDeviceStateLoggerRepository stateLoggerRepository,
  IDeviceStateLoggerFactory deviceLoggerFactory,
  ILogger<StateLoggerManager> logger,
  IDbContextFactory<CoreDatabaseContext> dbContextFactory,
  IAresDeviceProvider deviceProvider)
  {
    _stateLoggerRepository = stateLoggerRepository;
    _deviceLoggerFactory = deviceLoggerFactory;
    _logger = logger;
    _dbContextFactory = dbContextFactory;
    _deviceProvider = deviceProvider;
  }


  public void Initialize()
  {
    _deviceProvider.Connect()
      .SelectMany(async changes =>
      {
        foreach(var change in changes)
        {
          await HandleChangesAsync(change);
        }

        return Unit.Default;
      })
      .Subscribe()
      .DisposeWith(_cleanup);
  }

  private async Task HandleChangesAsync(Change<IAresDevice, string> change)
  {
    switch(change.Reason)
    {
      case ChangeReason.Add:
        await SetupLogger(change.Current);
        break;

      case ChangeReason.Remove:
        await RemoveLogger(change.Current.UniqueId);
        break;
    }
  }

  public async Task SetupLogger(IAresDevice device)
  {
    await _overrideLock.WaitAsync();

    try
    {
      // Stop and remove any existing logger for the device
      if(_stateLoggerRepository.TryGetValue(device.UniqueId, out var existingLogger))
      {
        await existingLogger.Stop();
        _stateLoggerRepository.Remove(device.UniqueId);
      }

      if(_deviceLoggerFactory is null)
      {
        _logger.LogError("No suitable logger factory found for device type {DeviceType}", device.GetType().Name);
        return;
      }

      var logger = _deviceLoggerFactory.Create(device);

      using var ctx = _dbContextFactory.CreateDbContext();
      var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == device.UniqueId);
      var settings = _overrideActive
        ? CreateOverrideSettings(device.UniqueId)
        : existingSettings ?? CreateDisabledSettings(device.UniqueId);

      await logger.Start(settings);
      _stateLoggerRepository.Add(device.UniqueId, logger);
    }
    finally
    {
      _overrideLock.Release();
    }
  }

  public async Task RemoveLogger(string deviceId, bool removeSettings = false)
  {
    await _overrideLock.WaitAsync();

    try
    {
      if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
      {
        await logger.Stop();
        _stateLoggerRepository.Remove(deviceId);
      }

      if(removeSettings)
      {
        using var ctx = _dbContextFactory.CreateDbContext();
        var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
        if(existingSettings is not null)
        {
          ctx.DeviceLoggingSettings.Remove(existingSettings);
          await ctx.SaveChangesAsync();
        }

        _logger.LogInformation("Removed logger and settings for device id {DeviceId}", deviceId);
        return;
      }

      _logger.LogInformation("Removed logger for device id {DeviceId}", deviceId);
    }
    finally
    {
      _overrideLock.Release();
    }
  }

  public async Task UpdateLogger(string deviceId, DeviceLoggingSettings settings)
  {
    await _overrideLock.WaitAsync();

    try
    {
      if(!_stateLoggerRepository.TryGetValue(deviceId, out var logger))
      {
        throw new KeyNotFoundException($"No logger found for device {deviceId}");
      }

      var deviceSettings = settings.Clone();
      deviceSettings.DeviceId = deviceId;
      await UpdateDatabase(deviceId, deviceSettings);

      if(!_overrideActive)
      {
        await logger.UpdateSettings(deviceSettings);
        _logger.LogInformation("Updated logger for device id {DeviceId}", deviceId);
      }
    }
    catch(Exception e)
    {
      _logger.LogError("Error updating device state logger for device {DeviceId}: {Exception}", deviceId, e);
      throw;
    }
    finally
    {
      _overrideLock.Release();
    }
  }

  private async Task UpdateDatabase(string deviceId, DeviceLoggingSettings settings)
  {
    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    if(existingSettings is not null)
    {
      existingSettings.IntervalMs = settings.IntervalMs;
      existingSettings.LoggingType = settings.LoggingType;
      existingSettings.Deltas.Clear();
      existingSettings.Deltas.Add(settings.Deltas);
      existingSettings.LoggingEnabled = settings.LoggingEnabled;
    }
    else
    {
      ctx.DeviceLoggingSettings.Add(settings);
    }

    await ctx.SaveChangesAsync();
  }

  public DeviceLoggingSettings GetCurrentLoggerSettings(string deviceId)
  {
    if(_stateLoggerRepository.TryGetValue(deviceId, out var logger))
    {
      return logger.Settings;
    }

    throw new KeyNotFoundException($"No logger found for device {deviceId}");
  }

  public async Task<DeviceLoggingSettings?> GetDatabaseLoggerSettings(string deviceId)
  {
    using var ctx = _dbContextFactory.CreateDbContext();
    var existingSettings = await ctx.DeviceLoggingSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId);
    return existingSettings;
  }

  public async Task DisableOverrideAsync()
  {
    _logger.LogDebug("Attempting to disable the device logging override");
    await _overrideLock.WaitAsync();

    if(!_overrideActive)
    {
      _logger.LogDebug("Releasing device logging override");
      _overrideLock.Release();
      return;
    }

    try
    {
      _logger.LogDebug("Preparing the tasks to disable device logging override");
      var loggerRestoreTasks = _stateLoggerRepository.Select(async logger =>
      {
        var settings = await GetDatabaseLoggerSettings(logger.Key) ?? CreateDisabledSettings(logger.Key);
        await logger.Value.UpdateSettings(settings);
      });

      await Task.WhenAll(loggerRestoreTasks);
      _overrideActive = false;
      _overrideSettings = null;
      _logger.LogInformation("Disabled state logging override");
    }
    catch(Exception e)
    {
      _logger.LogError("Error disabling state logging override: {Exception}", e);
      throw;
    }
    finally
    {
      _overrideLock.Release();
      _logger.LogDebug("Released the device logging override lock. (Disable method)");
    }
  }

  public async Task EnableOverrideAsync(DeviceLoggingSettings settings, bool loggingEnabled)
  {
    _logger.LogInformation("Attempting to enable device logging override.");
    await _overrideLock.WaitAsync();

    var previousOverrideActive = _overrideActive;
    var previousOverrideSettings = _overrideSettings;
    _overrideSettings = settings.Clone();
    _overrideSettings.LoggingEnabled = loggingEnabled;
    _overrideActive = true;

    try
    {
      _logger.LogDebug("Preparing the tasks to enable device logging override");
      var loggerOverrideTasks = _stateLoggerRepository
        .Select(logger => logger.Value.UpdateSettings(CreateOverrideSettings(logger.Key)));

      await Task.WhenAll(loggerOverrideTasks);
      _logger.LogInformation("Enabled state logging override");
    }
    catch(Exception e)
    {
      _logger.LogError("Error enabling state logging override: {Exception}", e);
      _overrideActive = previousOverrideActive;
      _overrideSettings = previousOverrideSettings;

      try
      {
        var loggerRollbackTasks = _stateLoggerRepository.Select(async logger =>
        {
          var rollbackSettings = previousOverrideActive
            ? CreateOverrideSettings(logger.Key)
            : await GetDatabaseLoggerSettings(logger.Key) ?? CreateDisabledSettings(logger.Key);
          await logger.Value.UpdateSettings(rollbackSettings);
        });

        await Task.WhenAll(loggerRollbackTasks);
      }
      catch(Exception rollbackException)
      {
        _logger.LogError("Error rolling back state logging override: {Exception}", rollbackException);
        throw new AggregateException(e, rollbackException);
      }

      throw;
    }
    finally
    {
      _overrideLock.Release();
      _logger.LogDebug("Released the device logging override lock. (Enable method)");
    }
  }

  private DeviceLoggingSettings CreateOverrideSettings(string deviceId)
  {
    var settings = _overrideSettings?.Clone() ?? CreateDisabledSettings(deviceId);
    settings.DeviceId = deviceId;
    return settings;
  }

  private static DeviceLoggingSettings CreateDisabledSettings(string deviceId)
  {
    return new DeviceLoggingSettings
    {
      DeviceId = deviceId,
      LoggingEnabled = false,
      LoggingType = DeviceLoggingSettings.Types.LoggingType.None
    };
  }
}
