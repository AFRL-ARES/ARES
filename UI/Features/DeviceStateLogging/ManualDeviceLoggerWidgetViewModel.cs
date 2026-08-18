using System.Windows.Input;
using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using DeviceStateRequest = Ares.Services.DeviceStateRequest;

namespace UI.Features.DeviceStateLogging;

public class ManualDeviceLoggerWidgetViewModel : ReactiveObject
{
  private readonly Ares.Core.Grpc.Services.DeviceStateExportService _stateExportClient;
  readonly DevicesService _devicesClient;
  private IEnumerable<string> _activeDevices = Array.Empty<string>();

  public ManualDeviceLoggerWidgetViewModel(Ares.Core.Grpc.Services.DeviceStateExportService stateExportClient, DevicesService devicesClient)
  {
    _stateExportClient = stateExportClient;
    _devicesClient = devicesClient;
    StartDataCollectionCommand = ReactiveCommand.CreateFromTask(StartDataCollection);
    StopDataCollectionCommand = ReactiveCommand.Create(StopDataCollection);
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
    var devicesResponse = await _devicesClient.ListAresDevices(new Empty(), null);
    _activeDevices = devicesResponse.AresDevices.Select(dev => dev.UniqueId).ToList();
  }

  public void StopDataCollection()
  {
    CollectionFinished = DateTime.UtcNow;
    IsCollecting = false;
    HasData = true;
  }

  public async Task<byte[]> GetExportData()
  {
    var reqFilter = new DeviceStateRequestFilter
    {
      Start = CollectionStarted.ToTimestamp(),
      End = CollectionFinished.ToTimestamp()
    };

    reqFilter.DeviceIds.AddRange(_activeDevices);
    var stateReq = new DeviceStateRequest { Filter = reqFilter, ExportType = ExportType.Combined };

    var result = await _stateExportClient.GetStateExport(stateReq, null);

    return result.Data.ToArray();
  }
}
