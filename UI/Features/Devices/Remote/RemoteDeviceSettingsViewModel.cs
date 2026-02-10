using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using UI.Features.Devices.Shared;
using UI.Domain.Notifications;

namespace UI.Features.Devices.Remote;

public partial class RemoteDeviceSettingsViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;
  private readonly DeviceInfo _deviceInfo;
  private readonly ObservableAsPropertyHelper<bool> _isBusy;
  private readonly IMessenger _deviceDeletionMessenger;

  public RemoteDeviceSettingsViewModel(AresDevices.AresDevicesClient devicesService,
      INotificationReceivingService notificationService,
      DeviceInfo deviceInfo,
      IMessenger deviceDeletionMessenger,
      Func<Task> onRemoveCallback)
  {
    _devicesClient = devicesService;
    _notificationService = notificationService;
    _deviceInfo = deviceInfo;
    _deviceDeletionMessenger = deviceDeletionMessenger;

    Name = _deviceInfo.Name;
    Address = _deviceInfo.Url;
    Type = _deviceInfo.Type;
    DeviceCommands = _deviceInfo.Commands.ToArray();
    Version = "";
    Description = "";
    StateMessage = "";
    SettingsSchema = new();
    Settings = new();

    var config = new RemoteDeviceConfig { Name = deviceInfo.Name, UniqueId = deviceInfo.UniqueId, Url = deviceInfo.Url };
    EditViewModel = new RemoteDeviceConfigEditViewModel(config);
    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
    RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
    FetchSettingsCommand = ReactiveCommand.CreateFromTask(FetchSettingsAsync);
    PushSettingsCommand = ReactiveCommand.CreateFromTask(PushSettingsAsync);
    UpdateStateCommand = ReactiveCommand.CreateFromTask(UpdateStateAsync);
    UpdateInfoCommand = ReactiveCommand.CreateFromTask(UpdateInfoAsync);

    var allExceptions = Observable.Merge(
        SaveCommand.ThrownExceptions,
        RemoveCommand.ThrownExceptions,
        FetchSettingsCommand.ThrownExceptions,
        PushSettingsCommand.ThrownExceptions,
        UpdateStateCommand.ThrownExceptions,
        UpdateInfoCommand.ThrownExceptions);

    allExceptions.Subscribe(HandleError);

    _isBusy = Observable.Merge(
            SaveCommand.IsExecuting,
            RemoveCommand.IsExecuting,
            UpdateStateCommand.IsExecuting,
            FetchSettingsCommand.IsExecuting,
            PushSettingsCommand.IsExecuting,
            UpdateInfoCommand.IsExecuting)
        .ToProperty(this, x => x.IsBusy);
  }

  [Reactive]
  public partial string Name { get; private set; }
  [Reactive]
  public partial string Address { get; private set; }
  [Reactive]
  public partial string Type { get; private set; }
  [Reactive]
  public partial string Version { get; private set; }
  [Reactive]
  public partial string Description { get; set; }
  [Reactive]
  public partial OperationalState OperationalState { get; private set; }
  [Reactive]
  public partial string StateMessage { get; private set; }
  [Reactive]
  public partial AresDataSchema SettingsSchema { get; private set; }
  [Reactive]
  public partial AresStruct Settings { get; private set; }
  [Reactive]
  public partial bool DeviceActive { get; private set; }
  [Reactive]
  public partial DeviceCommandDescriptor[] DeviceCommands { get; private set; }
  public RemoteDeviceConfigEditViewModel EditViewModel { get; }
  public bool IsBusy => _isBusy.Value;

  public ReactiveCommand<Unit, Unit> SaveCommand { get; }
  public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
  public ReactiveCommand<Unit, Unit> UpdateStateCommand { get; }
  public ReactiveCommand<Unit, Unit> UpdateInfoCommand { get; }
  public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }
  public ReactiveCommand<Unit, Unit> PushSettingsCommand { get; }

  private async Task SaveAsync()
  {
    var deviceConfig = EditViewModel.Save();
    var request = new UpdateRemoteDeviceRequest()
    {
      DeviceId = _deviceInfo.UniqueId,
      Name = deviceConfig.Name,
      Url = deviceConfig.Url
    };
    var response = await _devicesClient.UpdateRemoteDeviceAsync(request);
    if(response.Success)
    {
      PushNotification(new AresNotification
      {
        Title = "Device Update",
        Message = $"Device {deviceConfig.Name} updated successfully.",
        NotificationSeverity = Severity.Success
      });
      // Refresh local state from the server
      await UpdateInfoCommand.Execute();
      await UpdateStateCommand.Execute();
    }
    else
    {
      PushNotification(new AresNotification
      {
        Title = "Device Update Failed",
        Message = $"Device {deviceConfig.Name} failed to update: {response.ErrorMessage}",
        NotificationSeverity = Severity.Error
      });
    }
  }

  private async Task RemoveAsync(Func<Task> onRemoveCallback)
  {
    var request = new RemoveRemoteDeviceRequest() { DeviceId = _deviceInfo.UniqueId };
    _deviceDeletionMessenger.Send(new DeviceDeletedMessage(_deviceInfo.UniqueId));
    await _devicesClient.RemoveRemoteDeviceAsync(request);
    await onRemoveCallback();
  }

  private async Task UpdateStateAsync()
  {
    var request = new DeviceStatusRequest() { DeviceId = _deviceInfo.UniqueId };
    var stateResponse = await _devicesClient.GetDeviceStatusAsync(request);
    StateMessage = stateResponse.Message;
    OperationalState = stateResponse.OperationalState;
  }

  private async Task FetchSettingsAsync()
  {
    var request = new DeviceSettingsRequest() { DeviceId = _deviceInfo.UniqueId };
    var deviceSettings = await _devicesClient.GetDeviceSettingsAsync(request);
    Settings = deviceSettings;
  }

  private async Task PushSettingsAsync()
  {
    var settings = new DeviceSettings() { DeviceId = _deviceInfo.UniqueId, Settings = Settings };
    try
    {
      await _devicesClient.SetDeviceSettingsAsync(settings);
    }
    catch(Exception e)
    {
      PushNotification(new AresNotification { Title = "Update Error", Message = $"Settings for {Name} failed to send. {e.Message}", NotificationSeverity = Severity.Error });
    }
  }

  private async Task UpdateInfoAsync()
  {
    var request = new DeviceInfoRequest() { DeviceId = _deviceInfo.UniqueId };
    var infoResponse = await _devicesClient.GetDeviceInfoAsync(request);
    Type = infoResponse.Type;
    Name = infoResponse.Name;
    Address = infoResponse.Url;
    Version = infoResponse.Version;
    Description = infoResponse.Description;
    SettingsSchema = infoResponse.SettingsSchema ?? new AresDataSchema();
  }

  public async Task<DeviceOperationalStatus> GetOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceInfo.UniqueId }).ResponseAsync;
      DeviceActive = status.OperationalState is OperationalState.Active;
      return status;
    }
    catch(RpcException)
    {
      return new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered device with a name {_deviceInfo.Name}" };
    }
  }

  private void HandleError(Exception ex)
  {
    var errorMessage = ex is RpcException rpcEx
        ? $"A network error occurred: {rpcEx.Status.Detail}"
        : $"An unexpected error occurred: {ex.Message}";

    StateMessage = errorMessage;
    OperationalState = OperationalState.Error;

    PushNotification(new AresNotification
    {
      Title = "Operation Failed",
      Message = errorMessage,
      NotificationSeverity = Severity.Error
    });
  }

  private void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);
}

