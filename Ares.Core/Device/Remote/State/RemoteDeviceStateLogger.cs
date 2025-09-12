
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
public class RemoteDeviceStateLogger : IDeviceStateLogger
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly RemoteDevice _device;
  private readonly ILogger<RemoteDeviceStateLogger> _logger;
  private IDisposable _stateWatcher = Disposable.Empty;

  public RemoteDeviceStateLogger(IDbContextFactory<CoreDatabaseContext> dbContextFactory, RemoteDevice device, ILogger<RemoteDeviceStateLogger> logger)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
    _logger = logger;
  }

  public string DeviceId => _device.UniqueId;

  public Task Start(DeviceLoggingSettings? settings = null)
  {
    settings ??= new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

    var stream = _device.StateStream;
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

  private async Task UpdateState(AresStruct? state)
  {
    if(state is null)
    {
      return;
    }
    using var context = _dbContextFactory.CreateDbContext();

    var time = DateTime.UtcNow;
    var deviceState = new DeviceState
    {
      Timestamp = time.ToTimestampUtc(),
      DeviceId = _device.UniqueId,
      Data = state,
    };

    context.DeviceStates.Add(deviceState);
    try
    {
      await context.SaveChangesAsync();
    }
    catch(Exception ex)
    {
      _logger.LogError("Failed to save device state for mfc {DeviceName}. {Exception}", _device.Name, ex);
    }
  }
}
