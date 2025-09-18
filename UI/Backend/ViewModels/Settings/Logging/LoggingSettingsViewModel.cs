using System.Reactive;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Logging;

public class LoggingSettingsViewModel : ReactiveObject
{
  private readonly string _deviceId;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly ObservableAsPropertyHelper<bool> _updatedObservable;

  public LoggingSettingsViewModel(string deviceId, string deviceName, AresDevices.AresDevicesClient devicesClient)
  {
    _deviceId = deviceId;
    DeviceName = deviceName;
    _devicesClient = devicesClient;

    this.WhenAnyValue(vm => vm.IntervalMs,
      vm => vm.LoggingType,
      vm => vm.CurrentSettings,
      (interval, logType, settings) =>
        settings is not null && (interval != settings.IntervalMs || logType != settings.LoggingType)
    ).ToProperty(this, vm => vm.Updated, out _updatedObservable);

    FetchSettingsCommand = ReactiveCommand.CreateFromTask(FetchSettings);
  }
  public string DeviceName { get; }

  [Reactive]
  public bool Fetched { get; private set; }

  [Reactive]
  public DeviceLoggingSettings.Types.LoggingType LoggingType { get; set; }

  [Reactive] 
  private DeviceLoggingSettings? CurrentSettings { get; set; }
  
  [Reactive]
  public long IntervalMs { get; set; }

  public bool Updated => _updatedObservable.Value;

  public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }

  public async Task FetchSettings()
  {
    var settings = await _devicesClient.GetDeviceLoggerSettingsAsync(new DeviceLoggerSettingsRequest { DeviceId = _deviceId });

    CurrentSettings = settings;
    IntervalMs = settings.IntervalMs;
    LoggingType = settings.LoggingType;

    Fetched = true;
  }

  public async Task<bool> Save()
  {
    if(!Updated)
    {
      return false;
    }

    var settings = new DeviceLoggingSettings
    {
      DeviceId = _deviceId,
      IntervalMs = IntervalMs,
      LoggingType = LoggingType,
    };
    await _devicesClient.SetDeviceLoggerSettingsAsync(settings);
    await FetchSettings();
    return true;
  }
}
