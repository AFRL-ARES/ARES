using Ares.Core.Device.Providers;
using Ares.Core.Device.Sila;
using Ares.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using Tecan.Sila2;
using UI.Application.Notifications;

namespace UI.Features.Devices.Sila;

public partial class SilaDeviceSettingsListViewModel : ReactiveObject
{
  private readonly INotificationReceivingService _notificationService;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly ISilaDeviceManager _silaDeviceManager;

  public SilaDeviceSettingsListViewModel(IAresDeviceProvider deviceProvider, 
    ISilaDeviceManager silaDevice, 
    INotificationReceivingService notificationService)
  {
    _notificationService = notificationService;
    _deviceProvider = deviceProvider;
    _silaDeviceManager = silaDevice;
    SettingsViewModels = [];
  }

  public async Task UpdateAvailableDevices()
  {
    IsLoading = true;
    try
    {
      var silaDevices = _deviceProvider.GetAllDevices<SilaDevice>();
      UpdateViewModels(silaDevices);
    }
    catch(Exception e)
    {
      PushNotification(new AresNotification() 
      { 
        Message = $"Could not retrieve SiLA devices. {e.Message}", 
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

  public async Task SearchForSilaDevices()
  {
    IsLoading = true;
    await _silaDeviceManager.UpdateAvailableSilaDevices();
    await UpdateAvailableDevices();
  }

  public async Task AddNewSilaDevice(ServerData data)
    => await _silaDeviceManager.Create(data);
  

  public async Task<IEnumerable<ServerData>> SearchForSilaServers()
    => await _silaDeviceManager.UpdateAvailableSilaDevices();

  private void UpdateViewModels(IEnumerable<SilaDevice> silaDevices)
  {
    SettingsViewModels.Clear();
    var viewModels = silaDevices.Select(info => new SilaDeviceSettingsViewModel(info)).ToArray();
    foreach(var vm in viewModels)
    {
      SettingsViewModels.Add(vm);
    }
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public partial bool IsLoading { get; set; }

  public ObservableCollection<SilaDeviceSettingsViewModel> SettingsViewModels { get; }
}
