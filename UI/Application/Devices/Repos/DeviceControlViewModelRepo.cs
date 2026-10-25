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
using UI.Features.Devices.Sila.Factory;
using UI.Infrastructure.Devices.Remote;
using UI.Infrastructure.Devices.Sila;

namespace UI.Application.Devices.Repos
{
  public class DeviceControlViewModelRepo : IDeviceControlViewModelRepo
  {
    private readonly IAresDeviceProvider _deviceProvider;
    private readonly IRemoteDeviceAdapterRepository _remoteDeviceAdapterRepo;
    private readonly ISilaDeviceAdapterRepository _silaDeviceAdapterRepo;
    private readonly IAresDeviceViewModelFactory _factory;
    private readonly IRemoteDeviceControlViewModelFactory _remoteVmFactory;
    private readonly ISilaDeviceControlViewModelFactory _silaVmFactory;
    private readonly SourceCache<IDeviceUnitControlViewModel, string> _viewModelCache = new(vm => vm.DeviceId);
    private readonly CompositeDisposable _cleanup = new();

    public DeviceControlViewModelRepo(IAresDeviceProvider deviceProvider,
      IAresDeviceViewModelFactory factory,
      IRemoteDeviceControlViewModelFactory remoteVmFactory,
      IRemoteDeviceAdapterRepository remoteDeviceAdapterRepo,
      ISilaDeviceAdapterRepository silaDeviceAdapterRepo,
      ISilaDeviceControlViewModelFactory silaVmFactory)
    {
      _deviceProvider = deviceProvider;
      _factory = factory;
      _remoteDeviceAdapterRepo = remoteDeviceAdapterRepo;
      _silaDeviceAdapterRepo = silaDeviceAdapterRepo;
      _remoteVmFactory = remoteVmFactory;
      _silaVmFactory = silaVmFactory;
    }

    public void Initialize()
    {
      _deviceProvider.Connect()
        .Filter(IsPluginDevice)
        .Transform(_factory.CreateUnitControlViewModel)
        .DisposeMany()
        .PopulateInto(_viewModelCache)
        .DisposeWith(_cleanup);

      _silaDeviceAdapterRepo.Connect()
        .Transform(_silaVmFactory.Create)
        .DisposeMany()
        .PopulateInto(_viewModelCache)
        .DisposeWith(_cleanup);

      _remoteDeviceAdapterRepo.Connect()
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
