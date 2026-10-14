using Ares.Core.Device.Providers;
using Ares.Core.Visualization.Helpers;
using Ares.Core.Visualization.Managers;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Visualizing.Local;
using Ares.Datamodel.Visualizing.Remote;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using UI.Features.Visualization.ViewModels;

namespace Ares.Core.Visualization.ViewModels;

public partial class VisualizationViewModel : ReactiveObject, IDisposable
{
  private readonly IDeviceVisualizationConfigProvider _configProvider;
  private readonly IVisualizationConfigManager _visualizationConfigManager;
  private readonly IRemoteVisualizationConfigManager _remoteVisualizationConfigManager;
  private readonly IRemoteVisualizationConfigProvider _remoteVisualizationConfigProvider;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IDisposable _subscription;
  private readonly IDisposable _remoteSubscription;
  private readonly ReadOnlyObservableCollection<VisualizationItemViewModel> _visualizationItems;
  private readonly ReadOnlyObservableCollection<RemoteVisualizationItemViewModel> _remoteVisualizationItems;

  public VisualizationViewModel(
    IDeviceVisualizationConfigProvider configProvider,
    IRemoteVisualizationConfigProvider remoteConfigProvider,
    IAresDeviceProvider deviceProvider,
    IVisualizationConfigManager visualizationConfigManager,
    IRemoteVisualizationConfigManager remoteVisualizationConfigManager,
    IRemoteVisualizationDataProvider remoteDataProvider)
  {
    _configProvider = configProvider;
    _remoteVisualizationConfigProvider = remoteConfigProvider;
    _remoteVisualizationConfigManager = remoteVisualizationConfigManager;
    _deviceProvider = deviceProvider;
    _visualizationConfigManager = visualizationConfigManager;

    _subscription = _configProvider.Connect()
      .TransformWithInlineUpdate(
        config =>
        {
          var devices = config.GetAssociatedDeviceIds().Select(_deviceProvider.GetDevice).Where(d => d is not null);

          if(devices is null)
            return new VisualizationItemViewModel(config, [], OnChartDeleteRequested, OnChartUpdated);

          return new VisualizationItemViewModel(config, devices!, OnChartDeleteRequested, OnChartUpdated);
        },

        (existingViewModel, updatedConfig) =>
        {
          existingViewModel.UpdateFromConfig(updatedConfig);
        })
    .DisposeMany()
    .Bind(out _visualizationItems)
    .Subscribe(_ => this.RaisePropertyChanged(nameof(VisualizationItems)));

    _remoteSubscription = remoteConfigProvider.Connect()
      .TransformWithInlineUpdate(
        config => new RemoteVisualizationItemViewModel(
          config,
          remoteDataProvider,
          OnRemoteChartDeleteRequested,
          OnRemoteChartUpdated),
        (existingViewModel, updatedConfig) => existingViewModel.UpdateFromConfig(updatedConfig))
      .DisposeMany()
      .Bind(out _remoteVisualizationItems)
      .Subscribe(_ => this.RaisePropertyChanged(nameof(RemoteVisualizationItems)));
  }

  public void Dispose()
  {
    _subscription?.Dispose();
    _remoteSubscription?.Dispose();
    GC.SuppressFinalize(this);
  }

  private void OnChartDeleteRequested(string uniqueId)
    => _visualizationConfigManager.Remove(uniqueId);

  private void OnChartUpdated(string uniqueId, DeviceVisualizationConfig config)
    => _visualizationConfigManager.UpdateDeviceVisualization(uniqueId, config);

  private void OnRemoteChartDeleteRequested(string uniqueId)
    => _remoteVisualizationConfigManager.Remove(uniqueId);

  private void OnRemoteChartUpdated(string uniqueId, RemoteVisualizationConfig config)
    => _remoteVisualizationConfigManager.Update(config);

  public void UpdateChartPosition(string id, int x, int y, int w, int h)
  {
    var matchingChart = _configProvider.GetConfig(id);

    if(matchingChart is not null)
    {
      matchingChart.GridX = x;
      matchingChart.GridY = y;
      matchingChart.GridW = w;
      matchingChart.GridH = h;

      _visualizationConfigManager.UpdateDeviceVisualization(id, matchingChart);
      return;
    }

    var remoteChart = _remoteVisualizationItems.FirstOrDefault(chart => chart.UniqueId == id);
    if(remoteChart is not null)
    {
      var config = _remoteVisualizationConfigProvider.GetConfig(id);
      if(config is null)
        return;

      config.GridX = x;
      config.GridY = y;
      config.GridW = w;
      config.GridH = h;
      _remoteVisualizationConfigManager.Update(config);
    }
  }
  
  public ReadOnlyObservableCollection<VisualizationItemViewModel> VisualizationItems => _visualizationItems;
  public ReadOnlyObservableCollection<RemoteVisualizationItemViewModel> RemoteVisualizationItems => _remoteVisualizationItems;
}