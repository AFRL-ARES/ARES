using Ares.Core.Device.Providers;
using Ares.Core.Device.Remote;
using Ares.Core.Device.Sila;
using Ares.Device;
using Ares.Toolkit.Device.UI;
using DynamicData;
using DynamicData.PLinq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using UI.Features.Devices;
using UI.Features.Devices.Remote.Factory;

namespace UI.Application.Devices.Repos
{
  public class DeviceControlViewModelRepo : IDeviceControlViewModelRepo
  {
    private readonly IAresDeviceProvider _deviceProvider;
    private readonly IDeviceAdapterRepository _deviceAdapterRepo;
    private readonly IAresDeviceViewModelFactory _factory;
    private readonly IRemoteDeviceControlViewModelFactory _remoteVmFactory;
    private readonly SourceCache<IDeviceUnitControlViewModel, string> _viewModelCache = new(vm => vm.DeviceId);
    private readonly CompositeDisposable _cleanup = new();

    public DeviceControlViewModelRepo(IAresDeviceProvider deviceProvider,
      IAresDeviceViewModelFactory factory,
      IRemoteDeviceControlViewModelFactory remoteVmFactory,
      IDeviceAdapterRepository deviceAdapterRepo)
    {
      _deviceProvider = deviceProvider;
      _factory = factory;
      _deviceAdapterRepo = deviceAdapterRepo;
      _remoteVmFactory = remoteVmFactory;
    }

    public void Initialize()
    {
      _deviceProvider.Connect()
        .Filter(IsPluginDevice)
        .Transform(_factory.CreateUnitControlViewModel)
        .DisposeMany()
        .PopulateInto(_viewModelCache)
        .DisposeWith(_cleanup);

      _deviceAdapterRepo.Connect()
        .Transform(_remoteVmFactory.Create)
        .DisposeMany()
        .PopulateInto (_viewModelCache)
        .DisposeWith(_cleanup);
    }

    private bool IsPluginDevice(IAresDevice device)
      => device is not RemoteDevice && device is not SilaDevice;

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
