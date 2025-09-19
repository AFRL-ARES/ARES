using System.Collections.Generic;
using System.Text.Json;
using Google.Protobuf;
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
  private readonly Dictionary<string, double> _lastDeltaValues = new(StringComparer.OrdinalIgnoreCase);
  private List<DeviceLoggingDelta> _eligibleDeltas = new();

  public string DeviceId => device.UniqueId;

  public DeviceLoggingSettings Settings { get; private set; } = new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

  public Task Start(DeviceLoggingSettings? settings = null)
  {
    Settings = settings ?? Settings;

    _eligibleDeltas = Settings.Deltas
      .Where(d => d is not null && d.Delta > 0 && !string.IsNullOrWhiteSpace(d.Key))
      .ToList();

    var stream = device.StateStream;
    if(Settings.LoggingType == DeviceLoggingSettings.Types.LoggingType.Interval)
    {
      var timer = Observable.Interval(
        Settings.IntervalMs > 0 ? TimeSpan.FromMilliseconds(Settings.IntervalMs) : TimeSpan.FromMilliseconds(1));
      _stateWatcher = timer
        .WithLatestFrom(stream, (_, state) => state)
        .SelectMany(meme => Observable.FromAsync(() => UpdateState(meme)))
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
        .Where(state => ShouldEmitByDeltas(state))
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

  private bool ShouldEmitByDeltas(AresStruct? state)
  {
    // If no state or no configured/eligible deltas, allow emission (fallback to original behavior)
    if (state is null || _eligibleDeltas.Count == 0)
    {
      return true;
    }

    bool anyExceeded = false;

    foreach (var d in _eligibleDeltas)
    {
      if (!TryGetDouble(state, d.Key, out var current))
      {
        // If key not found or not numeric, skip this key silently
        continue;
      }

      if (_lastDeltaValues.TryGetValue(d.Key, out var last))
      {
        if (Math.Abs(current - last) > d.Delta)
        {
          anyExceeded = true;
          _lastDeltaValues[d.Key] = current; // advance baseline for this key
        }
      }
      else
      {
        // First observation for this key establishes baseline and triggers an emit
        anyExceeded = true;
        _lastDeltaValues[d.Key] = current;
      }
    }

    return anyExceeded;
  }

  private static bool TryGetDouble(AresStruct state, string keyPath, out double value)
  {
    value = default;

    if (string.IsNullOrWhiteSpace(keyPath))
      return false;

    // Serialize to JSON using protobuf formatter, then walk it as JsonDocument.
    // This assumes numeric leaves are valid JSON numbers.
    string json;
    try
    {
      json = JsonFormatter.Default.Format(state);
    }
    catch
    {
      return false;
    }

    try
    {
      using var doc = JsonDocument.Parse(json);
      var current = doc.RootElement;

      foreach (var segment in keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
      {
        if (current.ValueKind == JsonValueKind.Object)
        {
          if (!current.TryGetProperty(segment, out current))
          {
            return false;
          }
        }
        else if (current.ValueKind == JsonValueKind.Array)
        {
          // Support numeric index for arrays
          if (!int.TryParse(segment, out var idx))
            return false;
          if (idx < 0 || idx >= current.GetArrayLength())
            return false;
          current = current[idx];
        }
        else
        {
          return false;
        }
      }

      switch (current.ValueKind)
      {
        case JsonValueKind.Number:
          return current.TryGetDouble(out value);
        case JsonValueKind.String:
          // Allow numeric strings
          return double.TryParse(current.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        default:
          return false;
      }
    }
    catch
    {
      return false;
    }
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
