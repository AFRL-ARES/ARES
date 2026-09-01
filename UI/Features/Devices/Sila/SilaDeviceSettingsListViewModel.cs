using Ares.Core.Device.Providers;
using Ares.Core.Device.Sila;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using Tecan.Sila2;
using UI.Application.Notifications;

namespace UI.Features.Devices.Sila;

public partial class SilaDeviceSettingsListViewModel : ReactiveObject
{
  private readonly IUiNotificationService _notificationService;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly ISilaDeviceManager _silaDeviceManager;

  public SilaDeviceSettingsListViewModel(IAresDeviceProvider deviceProvider, 
    ISilaDeviceManager silaDevice,
    IUiNotificationService notificationService)
  {
    _notificationService = notificationService;
    _deviceProvider = deviceProvider;
    _silaDeviceManager = silaDevice;
    SettingsViewModels = [];
    Address = "";
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
      PushNotification(new UiNotificationMessage() 
      { 
        Detail = $"Could not retrieve SiLA devices. {e.Message}", 
        Summary = "Connection Error", 
        Severity = UiNotificationSeverity.Error
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

  public async Task AddManualSilaDevice(string address, int port)
    => await _silaDeviceManager.Create(address, port);

  public async Task<IEnumerable<ServerData>> SearchForSilaServers()
    => await _silaDeviceManager.UpdateAvailableSilaDevices();

  private void UpdateViewModels(IEnumerable<SilaDevice> silaDevices)
  {
    SettingsViewModels.Clear();
    var viewModels = silaDevices.Select(info => new SilaDeviceSettingsViewModel(info, _silaDeviceManager, OnDeviceRemoved)).ToArray();
    foreach(var vm in viewModels)
    {
      SettingsViewModels.Add(vm);
    }
  }

  public async Task OnDeviceRemoved()
  {
    await UpdateAvailableDevices();
  }

  public void PushNotification(UiNotificationMessage notification) => _notificationService.Notify(notification);

  [Reactive]
  public partial bool IsLoading { get; set; }

  [Reactive]
  public partial string Address { get; set; }

  [Reactive]
  public partial int Port { get; set; }

  public ObservableCollection<SilaDeviceSettingsViewModel> SettingsViewModels { get; }
}
