using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Providers;
using Ares.Core.Grpc.Services;
using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using UI.Application.Notifications;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceSettingsListViewModel : ReactiveObject
{
  private readonly DevicesService _devicesService;
  private readonly IDeviceConfigProvider _configProvider;
  private readonly INotificationReceivingService _notificationService;

  public PluginDeviceSettingsListViewModel(DevicesService devicesClient, INotificationReceivingService notificationService, IDeviceConfigProvider configProvider)
  {
    _devicesService = devicesClient;
    _notificationService = notificationService;
    _configProvider = configProvider;
  }

  public async Task Initialize(DeviceDriver driver)
  {
    DeviceClassName = driver.Manifest.DeviceTypeName;
    Driver = driver;
    await UpdateAvailableDevices();
  }

  private async Task UpdateAvailableDevices()
  {
    IsLoading = true;
    try
    {
      var request = new DeviceConfigRequest { DeviceType = DeviceClassName };
      var response = _configProvider.GetAllConfigs().Where(c => c.DriverId == Driver.UniqueId).ToList();
      UpdateViewModels(response);
    }
    catch (Exception e)
    {
      _notificationService.PushNotification(new AresNotification
      {
          Message = $"Could not retrieve devices for {DeviceClassName}. {e.Message}",
          Title = "Connection Error",
          NotificationSeverity = Severity.Error
      });
      SettingsViewModels.Clear();
    }
    finally
    {
      IsLoading = false;
    }
  }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    SettingsViewModels.Clear();
    var viewModels = deviceConfigs.Select(device => new PluginDeviceSettingsViewModel(device, Driver, _devicesService, _notificationService, OnDeviceRemoved)).ToList();
    viewModels.ForEach(SettingsViewModels.Add);
  }

  public async Task AddNewPluginDevice(DeviceConfig config)
  {
    try
    {
      var request = new AddDeviceRequest { DeviceConfig = config, DeviceName = config.DeviceName };
      var response = await _devicesService.AddAresDevice(request, null);

      if(response.Success)
      {
        PushNotification(new AresNotification() 
        { 
          Message = $"Added new device {config.DeviceName}", 
          NotificationSeverity = Severity.Success, 
          Title = $"Successfully Added {config.DeviceName}" 
        });
        await UpdateAvailableDevices();
      }

      else
      {
        PushNotification(new AresNotification() 
        { 
          Message = $"Failed to add device {config.DeviceName}. {response.ErrorMessage}", 
          NotificationSeverity = Severity.Error,
          Title = "Error Trying to Add Device" 
        });
      }
    }

    catch (Exception e)
    {
      PushNotification(new AresNotification() 
      { 
        Message = $"Failed to add device {config.DeviceName}. {e.Message}", 
        Title = "Error", 
        NotificationSeverity = Severity.Error 
      });
    }
  }

  private async Task OnDeviceRemoved() 
    => await UpdateAvailableDevices();

  public PluginDeviceConfigEditViewModel GetNewConfigEditViewModel() 
    => new(new DeviceConfig(), Driver, true, _devicesService);
  
  public void PushNotification(AresNotification notification) 
    => _notificationService.PushNotification(notification);

  [Reactive]
  public partial string DeviceClassName { get; set; }

  public ObservableCollection<PluginDeviceSettingsViewModel> SettingsViewModels { get; } = [];

  [Reactive]
  public partial bool IsLoading { get; private set; }

  public DeviceDriver Driver { get; set; } = new DeviceDriver(string.Empty);
}