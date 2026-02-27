using Ares.Core.Analyzing;
using Ares.Core.Device.Managers;
using Ares.Core.Device.Remote;
using Ares.Core.Planning;
using System.Runtime.CompilerServices;
using UI.Application.Devices.Repos;
using UI.Application.Notifications;
using UI.Application.Settings;
using UI.Features.Devices.Remote;
using UI.Infrastructure.Devices;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DeviceAdapterManager _deviceAdapterManager;
  private readonly RemoteDeviceControlViewModelFactory _remoteDeviceViewModelFactory;
  private readonly DeviceDriverSyncManager _deviceDriverSyncManager;
  private readonly IRemoteAnalyzerManager _analyzerManager;
  private readonly IRemotePlannerManager _plannerManager;
  private readonly IRemoteDeviceManager _remoteDeviceManager;
  private readonly IDeviceManager _deviceManager;
  private readonly IConfiguration _configuration;

  private readonly string _dataPath;
  private readonly string _resultsPath;
  private readonly string _templatesPath;
  private readonly string _devicesPath;

  public ServiceStarter(
    IRemotePlannerManager plannerManager,
    IRemoteAnalyzerManager analyzerManager,
    IConfiguration configuration,
    IRemoteDeviceManager remoteDeviceManager,
    IDeviceManager deviceManager,
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    DeviceAdapterManager deviceAdapterManager,
    RemoteDeviceControlViewModelFactory remoteDeviceViewModelFactory,
    DeviceDriverSyncManager deviceDriverSyncManager)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _deviceAdapterManager = deviceAdapterManager;
    _remoteDeviceViewModelFactory = remoteDeviceViewModelFactory;
    _deviceDriverSyncManager = deviceDriverSyncManager;

    _analyzerManager = analyzerManager;
    _plannerManager = plannerManager;
    _remoteDeviceManager = remoteDeviceManager;
    _configuration = configuration;
    _deviceManager = deviceManager;

    _dataPath = _configuration.Get<AppSettings>()?.AresDataPath ?? "";
    _resultsPath = Path.Combine(_dataPath, AppSettings.ResultsFolder);
    _templatesPath = Path.Combine(_dataPath, AppSettings.TemplatesFolder);
    _devicesPath = Path.Combine(_dataPath, AppSettings.DevicesFolder);
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await _deviceDriverSyncManager.SyncDriversAsync();
    _notificationReceivingService.StartNotificationStream();
    _deviceControlViewModelRepo.Initialize();
    _deviceAdapterManager.Activate();
    _remoteDeviceViewModelFactory.Start(TimeSpan.FromSeconds(5));
    await _plannerManager.LoadPlanners();
    await _analyzerManager.LoadAnalyzers();
    await _remoteDeviceManager.LoadDevices();
    await _deviceManager.LoadDevices();
    await EnsureDataPathsExist();
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _deviceControlViewModelRepo.Dispose();
    await _deviceAdapterManager.DisposeAsync();
    await _remoteDeviceViewModelFactory.DisposeAsync();
  }

  public Task EnsureDataPathsExist()
  {
    Directory.CreateDirectory(_devicesPath);
    Directory.CreateDirectory(_resultsPath);
    Directory.CreateDirectory(_templatesPath);

    return Task.CompletedTask;
  }
}

