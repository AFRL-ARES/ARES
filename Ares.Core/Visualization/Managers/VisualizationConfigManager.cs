using Ares.Core.CoreDevice;
using Ares.Core.Device.Providers;
using Ares.Core.Notifications;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Device;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace Ares.Core.Visualization.Managers;

public class VisualizationConfigManager : IVisualizationConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private IDeviceVisualizationConfigRepo _deviceVisualizationConfigRepo;
  private IDeviceConfigProvider _deviceConfigProvider;
  private readonly ILogger<VisualizationConfigManager> _logger;
  private INotificationHandler _notificationHandler;
  private readonly CompositeDisposable _cleanup = new();

  public VisualizationConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IDeviceVisualizationConfigRepo deviceVisualizationConfigRepo,
    IDeviceConfigProvider deviceConfigProvider,
    ILogger<VisualizationConfigManager> logger,
    INotificationHandler notificationHandler)
  {
    _dbContextFactory = dbContextFactory;
    _deviceVisualizationConfigRepo = deviceVisualizationConfigRepo;
    _logger = logger;
    _notificationHandler = notificationHandler;
    _deviceConfigProvider = deviceConfigProvider;
  }

  public async Task Initialize()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfigs = await context.DeviceVisualizationConfigs.ToListAsync();
    existingConfigs.ForEach(_deviceVisualizationConfigRepo.AddOrUpdate);

    _deviceConfigProvider.Connect()
      .SelectMany(async changes =>
      {
        foreach(var change in changes)
        {
          await HandleChangeAsync(change);
        }
        return Unit.Default;
      })
      .Subscribe()
      .DisposeWith(_cleanup);
  }

  private async Task HandleChangeAsync(Change<DeviceConfig, string> change)
  {
    switch(change.Reason)
    {
      case ChangeReason.Remove:
        await HandleDeviceRemoved(change.Current.DeviceId);
        break;
    }
  }

  private async Task HandleDeviceRemoved(string deviceId)
  {
    var matchingVisualizations = _deviceVisualizationConfigRepo.Where(d => d.DeviceIds.Contains(deviceId)).ToList();

    if(!matchingVisualizations.Any())
      return;
      
    foreach(var config in matchingVisualizations)
    {
      //Single device display, simply remove
      if(config.DeviceIds.Count == 1)
        await Remove(config.UniqueId);

      //Multi-device Visualization, auto update with device removed
      else
      {
        config.DeviceIds.Remove(deviceId);
        var pathsCopy = config.Paths.ToList();
        config.Paths.Clear();
        config.Paths.AddRange(pathsCopy.Where(p => p.AssociatedDeviceName !=  deviceId));

        await Remove(config.UniqueId);
        await AddDeviceVisualization(config.Paths.ToList(), config.Style);
      }
    }
  }

  public async Task AddDeviceVisualization(List<VisualizationPath> paths, ChartStyle style)
  {
    var newConfig = new DeviceVisualizationConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      Style = style,
      NumberDisplayPoints = 20,
      PollingRate = 3000,
      ShowDataLabels = true,
      ShowMarkers = true
    };

    newConfig.DeviceIds.AddRange(paths.Select(p => p.AssociatedDeviceName));
    newConfig.Paths.AddRange(paths);

    try
    {
      await using var context = _dbContextFactory.CreateDbContext();
      context.DeviceVisualizationConfigs.Add(newConfig);
      await context.SaveChangesAsync();
      _deviceVisualizationConfigRepo.AddOrUpdate(newConfig);
    }

    catch(Exception ex)
    {
      _logger.LogError($"Error occured when trying to add a new device visualization chart: {ex.Message}");
      await _notificationHandler.HandleNotification("Failed to Add Visualization", "Device visualization could not be created, check logs for additional details.", NotificationSeverityEnum.Error);
    }
  }

  public async Task Remove(string configId)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceVisualizationConfigs.FirstOrDefaultAsync(config => config.UniqueId == configId);
    if(genericConfig is null)
      return;

    context.DeviceVisualizationConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
    _deviceVisualizationConfigRepo.Remove(configId);
  }

  public async Task UpdateDeviceVisualization(string configId, DeviceVisualizationConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfig = await context.DeviceVisualizationConfigs.FirstOrDefaultAsync(c => c.UniqueId == configId);
    if(existingConfig is null)
      return;

    //Chart Settings
    existingConfig.DeviceIds.Clear();
    existingConfig.DeviceIds.AddRange(config.DeviceIds);
    existingConfig.Style = config.Style;
    existingConfig.Paths.Clear();
    existingConfig.Paths.AddRange(config.Paths);
    existingConfig.PollingRate = config.PollingRate;
    existingConfig.ShowDataLabels = config.ShowDataLabels;
    existingConfig.NumberDisplayPoints = config.NumberDisplayPoints;
    existingConfig.ShowMarkers = config.ShowMarkers;
    existingConfig.ChartTitle = config.ChartTitle;

    //Gridstack Location Info
    existingConfig.GridX = config.GridX;
    existingConfig.GridY = config.GridY;
    existingConfig.GridW = config.GridW;
    existingConfig.GridH = config.GridH;

    await context.SaveChangesAsync();
    _deviceVisualizationConfigRepo.AddOrUpdate(existingConfig);
  }
}
