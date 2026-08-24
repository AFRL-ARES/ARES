using System.Reactive;
using System.Reactive.Linq;
using Ares.Services;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.DeviceStateLogging.Settings;

public partial class LoggingSettingsListViewModel : ReactiveObject
{
  private readonly DevicesService _devicesClient;
  private readonly IUiNotificationService _notificationService;
  private ObservableAsPropertyHelper<bool> _updated;

  public LoggingSettingsListViewModel(DevicesService devicesClient, IUiNotificationService notificationService)
  {
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    RefreshLoggers = ReactiveCommand.CreateFromTask(_ => FetchLoggers());

    this.WhenAnyValue(x => x.LoggingSettingsViewModels)
      .SelectMany(vms => vms?.Select(vm => vm.WhenAnyValue(x => x.Updated)) ?? [])
      .Merge()
      .Select(_ => LoggingSettingsViewModels?.Any(vm => vm.Updated) ?? false)
      .ToProperty(this, vm => vm.Updated, out _updated);
  }

  public async Task FetchLoggers()
  {
    var response = await _devicesClient.GetAllAvailableDevices(new Empty(), null);
    var filteredDevices = response.Devices.Where(d => d.DeviceId != "ARES-CORE-DEVICE");
    LoggingSettingsViewModels = filteredDevices.Select(d => new LoggingSettingsViewModel(d.DeviceId, d.DeviceName, _devicesClient)).ToArray();
  }

  [Reactive]
  public partial LoggingSettingsViewModel[]? LoggingSettingsViewModels { get; private set; }

  public ReactiveCommand<Unit, Unit> RefreshLoggers { get; }

  public bool Updated => _updated.Value;

  public async Task Save()
  {
    if(LoggingSettingsViewModels is null)
    {
      return;
    }

    var saved = await Task.WhenAll(LoggingSettingsViewModels.Select(lsvm => lsvm.Save()));
    if(saved.Any(s => s))
    {
      var notif = new UiNotificationMessage
      {
        Detail = "Logging updates saved successfully",
        Summary = "Logging Settings",
        Severity = UiNotificationSeverity.Success
      };

      _notificationService.Notify(notif);
    }
  }
}

