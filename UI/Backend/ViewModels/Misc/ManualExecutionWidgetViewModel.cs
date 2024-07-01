using System.Windows.Input;
using Ares.Messages.DeviceStates;
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using UI.Backend.DeviceStateExport.ExportStreamProviders;

namespace UI.Backend.ViewModels.Misc;

public class ManualExecutionWidgetViewModel : ReactiveObject
{
  private IDeviceStateExportStreamProvider _streamProvider;
  readonly AresDevices.AresDevicesClient _devicesClient;
  private IEnumerable<string> _activeDevices = Array.Empty<string>();

  public ManualExecutionWidgetViewModel(IEnumerable<IDeviceStateExportStreamProvider> streamProviders, AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
    StartDataCollectionCommand = ReactiveCommand.CreateFromTask(StartDataCollection);
    StopDataCollectionCommand = ReactiveCommand.Create(StopDataCollection);
    _streamProvider = streamProviders.OfType<CombinedDeviceStateExportStreamProvider>().First();
  }

  public ICommand StartDataCollectionCommand { get; }
  public ICommand StopDataCollectionCommand { get; }
  public DateTime CollectionStarted { get; private set; }
  public DateTime CollectionFinished { get; private set; }
  public bool IsCollecting { get; private set; }
  public bool HasData { get; private set; }

  public async Task StartDataCollection()
  {
    HasData = false;
    IsCollecting = true;
    CollectionStarted = DateTime.UtcNow;
    var devicesResponse = await _devicesClient.ListAresDevicesAsync(new Empty());
    _activeDevices = devicesResponse.AresDevices.Select(dev => dev.Name).ToList();
  }

  public void StopDataCollection()
  {
    CollectionFinished = DateTime.UtcNow;
    IsCollecting = false;
    HasData = true;
  }

  public Task<ExportStateStream> GetExportStream()
  {
    var req = new StateRequest
    {
      Start = CollectionStarted.ToTimestamp(),
      End = CollectionFinished.ToTimestamp()
    };
    req.DeviceIds.AddRange(_activeDevices);
    return _streamProvider.Export(req);
  }
}
