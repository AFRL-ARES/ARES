using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;
using UI.Backend.Devices;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceDirectorViewModel : ReactiveObject, IDisposable
{
  private readonly IDisposable _vmUpdater;
  private bool _disposed;

  public RemoteDeviceDirectorViewModel(DeviceAdapterRepository deviceAdapterRepository)
  {
    _vmUpdater = deviceAdapterRepository
        .Connect()
        .Transform(adapter => new RemoteDeviceUnitViewModel(adapter))
        .Bind(out ReadOnlyObservableCollection<RemoteDeviceUnitViewModel> vms)
        .Subscribe();

    RemoteDeviceUnitViewModels = vms;
  }

  public ReadOnlyObservableCollection<RemoteDeviceUnitViewModel> RemoteDeviceUnitViewModels { get; }

  public void Dispose()
  {
    if(_disposed) return;
    _vmUpdater.Dispose();
    _disposed = true;
  }
}
