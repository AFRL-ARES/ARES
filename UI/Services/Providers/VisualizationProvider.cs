using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace UI.Services.Providers;

public class VisualizationProvider : IVisualizationProvider
{
  private readonly DevicesService _devicesClient;
  private readonly CancellationTokenSource _cts = new CancellationTokenSource();
  private readonly PeriodicTimer _timer;
  private readonly BehaviorSubject<IReadOnlyList<DeviceInfo>> _availableDevicesSubject = new(new List<DeviceInfo>());
  private readonly ILogger<VisualizationProvider> _logger;

  public VisualizationProvider(DevicesService devicesClient, ILogger<VisualizationProvider> logger)
  {
    _devicesClient = devicesClient;
    _logger = logger;
    _timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
    _ = StartPollingLoopAsync();
  }

  private async Task StartPollingLoopAsync()
  {
    try
    {
      await GetAvailableDevices();

      while (await _timer.WaitForNextTickAsync(_cts.Token))
      {
        await GetAvailableDevices();
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogWarning("Encountered an Operation Canceled exception in visualization provider loop. Might be expected if shutting down.");
    }
    catch (Exception ex)
    {
      _logger.LogError($"Error Encountered in Visualization Provider loop! {ex.Message}");
    }
  }

  public async Task GetAvailableDevices()
  {
    try
    {
      var devices = await _devicesClient.ListAresDevices(new Empty(), null);
      _availableDevicesSubject.OnNext(devices.AresDevices.ToList());
    }

    catch (Exception ex)
    {
      _logger.LogError($"Error while trying to retrieve available devices: {ex.Message}");
    }
  }

  public async Task<AresDataSchema> GetDeviceStateOptions(string deviceId)
  {
    var schema = await _devicesClient.GetDeviceStateSchema(new DeviceStateSchemaRequest { DeviceId = deviceId }, null);
    return schema.Schema;
  }

  public void Dispose()
  {
    _cts.Cancel();
    _availableDevicesSubject?.Dispose();
    _timer.Dispose();
  }

  public IObservable<IReadOnlyList<DeviceInfo>> AvailableDevicesStream => _availableDevicesSubject.AsObservable();
}
