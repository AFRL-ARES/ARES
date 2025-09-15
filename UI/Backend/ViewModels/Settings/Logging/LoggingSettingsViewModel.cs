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
  private DeviceLoggingSettings? _currentSettings;

  public LoggingSettingsViewModel(string deviceId, string deviceName, AresDevices.AresDevicesClient devicesClient)
  {
    _deviceId = deviceId;
    DeviceName = deviceName;
    _devicesClient = devicesClient;

    FetchSettingsCommand = ReactiveCommand.CreateFromTask(FetchSettings);
  }
  public string DeviceName { get; }

  [Reactive]
  public bool Fetched { get; private set; }

  [Reactive]
  public DeviceLoggingSettings.Types.LoggingType LoggingType { get; set; }

  [Reactive]
  public long IntervalMS { get; set; }

  public bool Updated => IntervalMS != _currentSettings?.IntervalMs || LoggingType != _currentSettings?.LoggingType;

  public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }

  public async Task FetchSettings()
  {
    var settings = await _devicesClient.GetDeviceLoggerSettingsAsync(new DeviceLoggerSettingsRequest { DeviceId = _deviceId });

    _currentSettings = settings.Settings;
    IntervalMS = settings.Settings.IntervalMs;
    LoggingType = settings.Settings.LoggingType;

    Fetched = true;
  }
}