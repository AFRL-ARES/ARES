using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Datamodel.Device;
using GenericSerialDevice.Commands.Responses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestDevice;

public class RestDeviceStateLogger : IRestDeviceStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ISerialRestDevice _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public RestDeviceStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ISerialRestDevice device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.UniqueId;
  public DeviceLoggingSettings Settings { get; private set; } = new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

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

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  private async Task UpdateState(ReadDataResponse stateResponse)
  {
    var context = _dbContextFactory.CreateDbContext();
    context.RestDeviceStates.Add(stateResponse.ToStateMessage(_device));
    try
    {
      await context.SaveChangesAsync();
    }
    catch(SqlException e)
    {
      Debug.WriteLine($"Exception while saving Rest Device State: {e})");
    }
  }

  public async Task UpdateSettings(DeviceLoggingSettings? settings)
  {
    await Stop();
    await Start(settings);
  }
}
