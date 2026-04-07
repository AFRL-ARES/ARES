using Ares.Core.Device.Providers;
using Ares.Core.Visualization.Managers;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Visualizing.Local;
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
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IDisposable _subscription;
  private readonly ReadOnlyObservableCollection<VisualizationItemViewModel> _visualizationItems;

  public VisualizationViewModel(IDeviceVisualizationConfigProvider configProvider, IAresDeviceProvider deviceProvider, IVisualizationConfigManager visualizationConfigManager)
  {
    _configProvider = configProvider;
    _deviceProvider = deviceProvider;
    _visualizationConfigManager = visualizationConfigManager;

    _subscription = _configProvider.Connect()
      .TransformWithInlineUpdate(
        config =>
        {
          var device = _deviceProvider.GetDevice(config.DeviceId);

          return new VisualizationItemViewModel(config, device, OnChartDeleteRequested, OnChartUpdated);
        },

        (existingViewModel, updatedConfig) =>
        {
          existingViewModel.UpdateFromConfig(updatedConfig);
        })
    .DisposeMany()
    .Bind(out _visualizationItems)
    .Subscribe(_ => this.RaisePropertyChanged(nameof(VisualizationItems)));
  }

  public void Dispose()
  {
    _subscription?.Dispose();
    GC.SuppressFinalize(this);
  }

  private void OnChartDeleteRequested(string uniqueId)
    => _visualizationConfigManager.Remove(uniqueId);

  private void OnChartUpdated(string uniqueId, DeviceVisualizationConfig config)
    => _visualizationConfigManager.UpdateDeviceVisualization(uniqueId, config);

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
    }
  }
  
  public ReadOnlyObservableCollection<VisualizationItemViewModel> VisualizationItems => _visualizationItems;
}