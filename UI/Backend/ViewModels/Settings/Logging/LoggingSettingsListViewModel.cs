using System.Reactive;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.DeviceStateLogging;

namespace UI.Backend.ViewModels.Settings.Logging;

public class LoggingSettingsListViewModel : ReactiveObject
{
  private readonly ICombinedDeviceGetter _deviceGetter;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public LoggingSettingsListViewModel(ICombinedDeviceGetter deviceGetter, AresDevices.AresDevicesClient devicesClient)
  {
    _deviceGetter = deviceGetter;
    _devicesClient = devicesClient;
    RefreshLoggers = ReactiveCommand.CreateFromTask(_ => FetchLoggers());
  }

  public async Task FetchLoggers()
  {
    var devices = await _deviceGetter.GetAvailableDevices();
    LoggingSettingsViewModels = devices.Select(d => new LoggingSettingsViewModel(d.DeviceId, d.DeviceName, _devicesClient)).ToArray();
  }

  [Reactive]
  public LoggingSettingsViewModel[] LoggingSettingsViewModels { get; private set; } = [];

  public ReactiveCommand<Unit, Unit> RefreshLoggers { get; }
}