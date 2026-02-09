using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using FlirCM3;
using FlirCM3.Config;
using FlirCM3.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.CM3Camera;

namespace UI.Backend.ViewModels.Settings.Device.CM3Camera
{
  public partial class CM3CameraSettingsListViewModel : ReactiveObject
  {
    private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private IMessenger _messenger;

    public CM3CameraSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, FlirCM3CameraRpc.FlirCM3CameraRpcClient cameraClient, IMessenger messenger)
    {
      _client = cameraClient;
      _devicesClient = devicesClient;
      _messenger = messenger;
      UpdateConfigs();
    }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
      var viewModels = deviceConfigs.Select(config => new CM3CameraSettingsViewModel(config, _client, _devicesClient, _messenger, OnConfigRemoved));
      SettingsViewModels = viewModels;
    }

    public FlirCM3ConfigEditViewModel GetNewConfigEditViewModel()
      => new(_client, _devicesClient);

    private Task UpdateConfigs()
    {
      SettingsViewModels = null;
      return _devicesClient
        .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IFlirCM3Camera).FullName })
        .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
    }

    private async Task OnConfigRemoved()
    {
      SettingsViewModels = null;
      await UpdateConfigs();
    }

    public async Task AddNewConfig(FlirCM3Config config)
    {
      await _client.AddCM3CameraAsync(config);
      await UpdateConfigs();
    }

    [Reactive]
    public partial IEnumerable<CM3CameraSettingsViewModel>? SettingsViewModels { get; private set; }
  }
}
