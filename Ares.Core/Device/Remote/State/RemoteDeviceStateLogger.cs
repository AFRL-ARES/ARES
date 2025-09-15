
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Ares.Core.Device.State.Logging;
using Ares.Core.EntityConfigurations;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote.State;
public class RemoteDeviceStateLogger(
  IDbContextFactory<CoreDatabaseContext> dbContextFactory,
  RemoteDevice device,
  ILogger<RemoteDeviceStateLogger> logger)
  : IDeviceStateLogger
{
  private IDisposable _stateWatcher = Disposable.Empty;

  public string DeviceId => device.UniqueId;

  public Task Start(DeviceLoggingSettings? settings = null)
  {
    settings ??= new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

    var stream = device.StateStream;
    if(settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.Interval)
    {
      var timer = Observable.Interval(
        settings.IntervalMs > 0 ? TimeSpan.FromMilliseconds(settings.IntervalMs) : TimeSpan.FromMilliseconds(1));
      _stateWatcher = timer
        .CombineLatest(stream, (tick, state) => state)
        .SelectMany(meme => Observable.FromAsync(() => UpdateState(meme)))
        .OnErrorResumeNext(Observable.Empty<Unit>())
        .Subscribe();
    }
    else if(settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.OnChange)
    {
      if(settings.IntervalMs > 0)
      {
        stream = stream.Sample(TimeSpan.FromMilliseconds(settings.IntervalMs));
      }

      _stateWatcher = stream
        .SelectMany(meme => Observable.FromAsync(() => UpdateState(meme)))
        .OnErrorResumeNext(Observable.Empty<Unit>())
        .Subscribe();
    }

    return Task.CompletedTask;
  }

  public Task Stop()
  {
    _stateWatcher.Dispose();

    return Task.CompletedTask;
  }

  public async Task UpdateSettings(DeviceLoggingSettings settings)
  {
    await Stop();
    await Start(settings);
  }

  private async Task UpdateState(AresStruct? state)
  {
    if(state is null)
    {
      return;
    }
    using var context = dbContextFactory.CreateDbContext();

    var time = DateTime.UtcNow;
    var deviceState = new DeviceState
    {
      Timestamp = time.ToTimestampUtc(),
      DeviceId = device.UniqueId,
      Data = state,
    };

    context.DeviceStates.Add(deviceState);
    try
    {
      await context.SaveChangesAsync();
    }
    catch(Exception ex)
    {
      logger.LogError("Failed to save device state for mfc {DeviceName}. {Exception}", device.Name, ex);
    }
  }
}
