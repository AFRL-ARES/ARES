using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.EntityConfigurations;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates.Chiller;
using AresService.Data;
using LaserChiller;
using LaserChiller.Commands.Responses;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.LaserChiller;

public class LaserChillerStateLogger : ILaserChillerStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ILaserChiller _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public LaserChillerStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ILaserChiller device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.UniqueId;

  public DeviceLoggingSettings Settings { get; private set; } = new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  public void Dispose()
  {
    _stateWatcher?.Dispose();
  }

  public async Task Start(DeviceLoggingSettings? settings)
  {
    Settings = settings ?? Settings;

    _stateWatcher.Dispose();

    if(Settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.None)
    {
      _stateWatcher = Disposable.Empty;
      return;
    }

    using var context = _dbContextFactory.CreateDbContext();
    _ = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == _device.UniqueId && config.DeviceType == _device.GetType().FullName);

    var stream = _device.StateStream.Where(state => state is not null);

    if(Settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.Interval)
    {
      var timer = Observable.Interval(Settings.IntervalMs > 0 ? TimeSpan.FromMilliseconds(Settings.IntervalMs) : TimeSpan.FromMilliseconds(1));
      _stateWatcher = timer
        .WithLatestFrom(stream, (_, state) => state!)
        .SelectMany(state => Observable.FromAsync(() => UpdateState(state)))
        .OnErrorResumeNext(Observable.Empty<Unit>())
        .Subscribe();
    }
    else if(Settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.OnChange)
    {
      if(Settings.IntervalMs > 0)
      {
        stream = stream.Sample(TimeSpan.FromMilliseconds(Settings.IntervalMs));
      }

      _stateWatcher = stream
        .SelectMany(state => Observable.FromAsync(() => UpdateState(state!)))
        .OnErrorResumeNext(Observable.Empty<Unit>())
        .Subscribe();
    }
    else
    {
      _stateWatcher = Disposable.Empty;
    }
  }

  public async Task UpdateState(DateTime timestamp)
  {
    var state = await _device.StateStream.Take(1);
    await using var context = _dbContextFactory.CreateDbContext();
    var chillerState = new ChillerState
    {
      Timestamp = timestamp.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      ManifoldTemperature = state.Temperature,
      DeviceId = _device.UniqueId
    };

    context.ChillerStates.Add(chillerState);
    await context.SaveChangesAsync();
  }

  private async Task UpdateState(GetManifoldTemperatureResponse state)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var time = DateTime.UtcNow;

    var chillerState = new ChillerState
    {
      Timestamp = time.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      ManifoldTemperature = state?.Temperature,
      DeviceId = _device.UniqueId
    };

    context.ChillerStates.Add(chillerState);
    await context.SaveChangesAsync();
  }

  public async Task UpdateSettings(DeviceLoggingSettings? settings)
  {
    await Stop();
    await Start(settings);
  }
}
