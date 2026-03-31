using Ares.Core.Device.Providers;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Visualizing.Local;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using UI.Features.Visualization.ViewModels;

namespace Ares.Core.Visualization.ViewModels;

public partial class VisualizationViewModel : ReactiveObject, IDisposable
{
  private readonly IDeviceVisualizationConfigProvider _configProvider;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IDisposable _subscription;
  private readonly ReadOnlyObservableCollection<VisualizationItemViewModel> _visualizationItems;

  public VisualizationViewModel(IDeviceVisualizationConfigProvider configProvider, IAresDeviceProvider deviceProvider)
  {
    _configProvider = configProvider;
    _deviceProvider = deviceProvider;

    _subscription = _configProvider.Connect()
        // Transform the raw config into our rich, reactive Item ViewModel
        .Transform(config =>
        {
          var device = _deviceProvider.GetDevice(config.DeviceId);
          return new VisualizationItemViewModel(config, device);
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

  public ReadOnlyObservableCollection<VisualizationItemViewModel> VisualizationItems => _visualizationItems;
}