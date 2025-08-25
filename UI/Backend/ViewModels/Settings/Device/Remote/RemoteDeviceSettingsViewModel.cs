using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI;
using UI.Services.Notification;
using System.Reactive;
using System.Reactive.Linq;

namespace UI.Backend.ViewModels.Settings.Device.Remote;

public class RemoteDeviceSettingsViewModel : ReactiveObject
{
    private readonly AresDevices.AresDevicesClient _devicesService;
    private readonly INotificationReceivingService _notificationService;
    private readonly DeviceInfo _deviceInfo;
    private readonly ObservableAsPropertyHelper<bool> _isBusy;

    private string _name;
    private string _address;
    private string _type;
    private string _version = "";
    private string _description = "";
    private OperationalState _operationalState;
    private string _stateMessage = "";
    private AresDataSchema _settingsSchema = new();
    private AresStruct _settings = new();

    public RemoteDeviceSettingsViewModel(
        AresDevices.AresDevicesClient devicesService,
        INotificationReceivingService notificationService,
        DeviceInfo deviceInfo,
        Func<Task> onRemoveCallback)
    {
        _devicesService = devicesService;
        _notificationService = notificationService;
        _deviceInfo = deviceInfo;

        _name = _deviceInfo.Name;
        _address = _deviceInfo.Url;
        _type = _deviceInfo.Type;

        var config = new RemoteDeviceConfig { Name = deviceInfo.Name, UniqueId = deviceInfo.UniqueId, Url = deviceInfo.Url };
        EditViewModel = new RemoteDeviceConfigEditViewModel(config);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
        UpdateStateCommand = ReactiveCommand.CreateFromTask(UpdateStateAsync);
        FetchSettingsCommand = ReactiveCommand.CreateFromTask(FetchSettingsAsync);
        PushSettingsCommand = ReactiveCommand.CreateFromTask(PushSettingsAsync);
        UpdateInfoCommand = ReactiveCommand.CreateFromTask(UpdateInfoAsync);

        var allExceptions = Observable.Merge(
            SaveCommand.ThrownExceptions,
            RemoveCommand.ThrownExceptions,
            UpdateStateCommand.ThrownExceptions,
            FetchSettingsCommand.ThrownExceptions,
            PushSettingsCommand.ThrownExceptions,
            UpdateInfoCommand.ThrownExceptions);

        allExceptions.Subscribe(HandleError);

        _isBusy = Observable.Merge(
                SaveCommand.IsExecuting,
                RemoveCommand.IsExecuting,
                UpdateStateCommand.IsExecuting,
                UpdateInfoCommand.IsExecuting,
                FetchSettingsCommand.IsExecuting,
                PushSettingsCommand.IsExecuting)
            .ToProperty(this, x => x.IsBusy);

        // Initial data load
        UpdateInfoCommand.Execute().Subscribe();
        UpdateStateCommand.Execute().Subscribe();
        FetchSettingsCommand.Execute().Subscribe();
    }

    public string Name { get => _name; private set => this.RaiseAndSetIfChanged(ref _name, value); }
    public string Address { get => _address; private set => this.RaiseAndSetIfChanged(ref _address, value); }
    public string Type { get => _type; private set => this.RaiseAndSetIfChanged(ref _type, value); }
    public string Version { get => _version; private set => this.RaiseAndSetIfChanged(ref _version, value); }
    public string Description { get => _description; set => this.RaiseAndSetIfChanged(ref _description, value); }
    public OperationalState OperationalState { get => _operationalState; private set => this.RaiseAndSetIfChanged(ref _operationalState, value); }
    public string StateMessage { get => _stateMessage; private set => this.RaiseAndSetIfChanged(ref _stateMessage, value); }
    public AresDataSchema SettingsSchema { get => _settingsSchema; private set => this.RaiseAndSetIfChanged(ref _settingsSchema, value); }
    public AresStruct Settings { get => _settings; private set => this.RaiseAndSetIfChanged(ref _settings, value); }

    public RemoteDeviceConfigEditViewModel EditViewModel { get; }
    public bool IsBusy => _isBusy.Value;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateStateCommand { get; }
    public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> PushSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateInfoCommand { get; }

    private async Task SaveAsync()
    {
        var deviceConfig = EditViewModel.Save();
        var request = new UpdateRemoteDeviceRequest()
        {
            DeviceId = _deviceInfo.UniqueId,
            Name = deviceConfig.Name,
            Url = deviceConfig.Url
        };
        var response = await _devicesService.UpdateRemoteDeviceAsync(request);
        if (response.Success)
        {
            PushNotification(new AresNotification
            {
                Title = "Analyzer Update",
                Message = $"Analyzer {deviceConfig.Name} updated successfully.",
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
                Title = "Analyzer Update Failed",
                Message = $"Analyzer {deviceConfig.Name} failed to update: {response.ErrorMessage}",
                NotificationSeverity = Severity.Error
            });
        }
    }

    private async Task RemoveAsync(Func<Task> onRemoveCallback)
    {
        var request = new RemoveRemoteDeviceRequest() { DeviceId = _deviceInfo.UniqueId };
        await _devicesService.RemoveRemoteDeviceAsync(request);
        await onRemoveCallback();
    }

    private async Task UpdateStateAsync()
    {
        var request = new DeviceStatusRequest() { DeviceId = _deviceInfo.UniqueId };
        var stateResponse = await _devicesService.GetDeviceStatusAsync(request);
        StateMessage = stateResponse.Message;
        OperationalState = stateResponse.OperationalState;
    }

    private async Task FetchSettingsAsync()
    {
        var request = new DeviceSettingsRequest() { DeviceId = _deviceInfo.UniqueId };
        Settings = await _devicesService.GetDeviceSettingsAsync(request);
    }

    private async Task PushSettingsAsync()
    {
        var settings = new DeviceSettings() { DeviceId = _deviceInfo.UniqueId, Settings = Settings };
        await _devicesService.SetDeviceSettingsAsync(settings);
        PushNotification(new AresNotification { Title = "Settings Pushed", Message = $"Settings for {Name} have been sent.", NotificationSeverity = Severity.Info });
    }

    private async Task UpdateInfoAsync()
    {
        var request = new DeviceInfoRequest() { DeviceId = _deviceInfo.UniqueId };
        var infoResponse = await _devicesService.GetDeviceInfoAsync(request);
        Type = infoResponse.Type;
        Name = infoResponse.Name;
        Address = infoResponse.Url;
        Version = infoResponse.Version;
        Description = infoResponse.Description;
        SettingsSchema = infoResponse.SettingsSchema ?? new AresDataSchema();
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
