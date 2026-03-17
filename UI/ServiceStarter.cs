using Ares.Core.Analyzing;
using Ares.Core.Device.Managers;
using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Plugins.Drivers.Loading;
using Ares.Core.Device.Remote;
using Ares.Core.Planning;
using UI.Application.Devices.Repos;
using UI.Application.Notifications;
using UI.Application.Settings;
using UI.Infrastructure.Devices;
using UI.Infrastructure.Startup;

namespace UI;

public class ServiceStarter : BackgroundService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DeviceAdapterManager _deviceAdapterManager;
  private readonly IRemoteAnalyzerManager _analyzerManager;
  private readonly IRemotePlannerManager _plannerManager;
  private readonly IRemoteDeviceManager _remoteDeviceManager;
  private readonly IDeviceConfigManager _deviceConfigManager;
  private readonly IDeviceDriverLoader _deviceDriverLoader;
  private readonly IDeviceManager _deviceManager;
  private readonly IDriverDatabaseManager _driverDbManager;
  private readonly IConfiguration _configuration;
  private readonly StartupStateTracker _tracker;

  private readonly string _dataPath;
  private readonly string _resultsPath;
  private readonly string _templatesPath;
  private readonly string _devicesPath;
  private readonly string _pluginsPath;

  public ServiceStarter(
    IRemotePlannerManager plannerManager,
    IDeviceDriverLoader deviceDriverLoader,
    IRemoteAnalyzerManager analyzerManager,
    IDeviceConfigManager deviceConfigManager,
    IConfiguration configuration,
    IRemoteDeviceManager remoteDeviceManager,
    IDriverDatabaseManager driverDbManager,
    IDeviceManager deviceManager,
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    DeviceAdapterManager deviceAdapterManager,
    StartupStateTracker tracker)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _deviceAdapterManager = deviceAdapterManager;
    _deviceDriverLoader = deviceDriverLoader;

    _analyzerManager = analyzerManager;
    _plannerManager = plannerManager;
    _remoteDeviceManager = remoteDeviceManager;
    _configuration = configuration;
    _deviceManager = deviceManager;
    _deviceConfigManager = deviceConfigManager;
    _driverDbManager = driverDbManager;
    _tracker = tracker;

    _dataPath = _configuration.Get<AppSettings>()?.AresDataPath ?? "";
    _resultsPath = Path.Combine(_dataPath, AppSettings.ResultsFolder);
    _templatesPath = Path.Combine(_dataPath, AppSettings.TemplatesFolder);
    _devicesPath = Path.Combine(_dataPath, AppSettings.DevicesFolder);
    _pluginsPath = PluginPathResolver.Resolve(_configuration.Get<AppSettings>());
  }

  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    _notificationReceivingService.StartNotificationStream();

    var localTrack = Task.Run(async () =>
    {
      await _deviceDriverLoader.LoadModulesAsync(_pluginsPath);
      _deviceControlViewModelRepo.Initialize();
      _deviceManager.Initialize();
      _deviceAdapterManager.Activate();
      await _deviceConfigManager.LoadConfigs();
      //It's important that we run this last. The archive serves as our bridge to update replaced drivers.
      //If we overwrite it too early, our archive wipes out the references to the deleted drivers making
      //it impossible to migrate devices to updated drivers.
      await _driverDbManager.RefreshDriverArchive();
    }, cancellationToken);

    var infraTrack = EnsureDataPathsExist();

    var remoteTrack = Task.WhenAll(
      _plannerManager.LoadPlanners(),
      _analyzerManager.LoadAnalyzers(),
      _remoteDeviceManager.LoadDevices()
    );

    await Task.WhenAll(localTrack, infraTrack, remoteTrack);
    _tracker.MarkAsReady();
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _deviceControlViewModelRepo.Dispose();
    await _deviceAdapterManager.DisposeAsync();
  }

  public Task EnsureDataPathsExist()
  {
    Directory.CreateDirectory(_devicesPath);
    Directory.CreateDirectory(_resultsPath);
    Directory.CreateDirectory(_templatesPath);

    return Task.CompletedTask;
  }
}

