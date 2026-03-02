using Ares.Core.Device.Providers;
using Ares.Toolkit.Device.UI;
using DynamicData;
using DynamicData.PLinq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using UI.Features.Devices.Shared;

namespace UI.Application.Devices.Repos
{
  public class DeviceControlViewModelRepo : IDeviceControlViewModelRepo
  {
    private readonly IAresDeviceProvider _deviceProvider;
    private readonly IDeviceViewModelFactory _factory;
    private readonly SourceCache<IDeviceUnitControlViewModel, string> _viewModelCache = new(vm => vm.DeviceId);
    private readonly CompositeDisposable _cleanup = new();

    public DeviceControlViewModelRepo(IAresDeviceProvider deviceProvider, IDeviceViewModelFactory factory)
    {
      _deviceProvider = deviceProvider;
      _factory = factory;
    }

    public void Initialize()
    {
      _deviceProvider.Connect()
        .Transform(device => _factory.Create(device))
        .DisposeMany()
        .PopulateInto(_viewModelCache)
        .DisposeWith(_cleanup);
    }

    public IObservable<IChangeSet<IDeviceUnitControlViewModel>> Connect(Func<IDeviceUnitControlViewModel, bool>? predicate = null)
        => _viewModelCache.Connect().Filter(predicate ?? (_ => true)).RemoveKey();

    public IObservable<IChangeSet<IDeviceUnitControlViewModel>> Preview(Func<IDeviceUnitControlViewModel, bool>? predicate = null)
        => _viewModelCache.Preview(predicate).RemoveKey();

    public IObservable<int> CountChanged => _viewModelCache.CountChanged;

    public IEnumerable<IDeviceUnitControlViewModel> Items => _viewModelCache.Items;

    public int Count => _viewModelCache.Count;

    IReadOnlyList<IDeviceUnitControlViewModel> IObservableList<IDeviceUnitControlViewModel>.Items => _viewModelCache.Items.ToList();

    public void Dispose()
    {
      _cleanup.Dispose();
      _viewModelCache.Dispose();
    }
  }
}
