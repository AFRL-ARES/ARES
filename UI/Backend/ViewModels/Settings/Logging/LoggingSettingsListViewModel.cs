using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.ViewModels.DeviceStateLogging;

namespace UI.Backend.ViewModels.Settings.Logging;

public class LoggingSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly ICombinedDeviceGetter _deviceGetter;

  public LoggingSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, ICombinedDeviceGetter deviceGetter)
  {
    _devicesClient = devicesClient;
    _deviceGetter = deviceGetter;
    
    //ReactiveCommand.CreateFromTask(_ => _deviceGetter.GetAvailableDevices)
  }
  
}