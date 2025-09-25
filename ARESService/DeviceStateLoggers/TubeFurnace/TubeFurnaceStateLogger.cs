using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Datamodel.Device;
using LindbergFurnace;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Messaging;

namespace AresService.DeviceStateLoggers.TubeFurnace;

public class TubeFurnaceStateLogger : ITubeFurnaceStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ITubeFurnace _tubeFurnace;
  private IDisposable _stateWatcher = Disposable.Empty;

  public TubeFurnaceStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ITubeFurnace tubeFurnace)
  {
    _dbContextFactory = dbContextFactory;
    _tubeFurnace = tubeFurnace;
  }

  public string DeviceId => _tubeFurnace.UniqueId;

  public DeviceLoggingSettings Settings { get; private set; } = new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

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
    _ = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == _tubeFurnace.UniqueId && config.DeviceType == _tubeFurnace.GetType().FullName);

    var stream = _tubeFurnace.StateStream;

    if(Settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.Interval)
    {
      var timer = Observable.Interval(Settings.IntervalMs > 0 ? TimeSpan.FromMilliseconds(Settings.IntervalMs) : TimeSpan.FromMilliseconds(1));
      _stateWatcher = timer
        .WithLatestFrom(stream, (_, state) => state)
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
        .SelectMany(state => Observable.FromAsync(() => UpdateState(state)))
        .OnErrorResumeNext(Observable.Empty<Unit>())
        .Subscribe();
    }
    else
    {
      _stateWatcher = Disposable.Empty;
    }
  }

  private async Task UpdateState(TubeFurnaceState stateResponse)
  {
    var context = _dbContextFactory.CreateDbContext();
    var state = stateResponse.ToStateMessage();
    context.TubeFurnaceStates.Add(state);
    // sometimes the context times out for some reason and we don't want
    // to just crash the service. Although this only happened during debugging
    // so far, so this may not be a problem during normal use.
    try
    {
      await context.SaveChangesAsync();
    }
    catch(SqlException e)
    {
      Debug.WriteLine($"Exception while saving MFC State: {e})");
    }
  }

  public void Dispose()
  {
    _stateWatcher.Dispose();
  }

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  public async Task UpdateSettings(DeviceLoggingSettings? settings)
  {
    await Stop();
    await Start(settings);
  }
}
