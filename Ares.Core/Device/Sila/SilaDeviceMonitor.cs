using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Ares.Datamodel.Device;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Sila;

internal class SilaDeviceMonitor : IDisposable
{
  private readonly SilaDevice _device;
  private readonly Task _monitorTask;
  private readonly CancellationTokenSource _tokenSource;
  private OperationalState _lastState = OperationalState.Unspecified;
  private readonly ILogger<SilaDeviceMonitor> _logger;

  public SilaDeviceMonitor(SilaDevice device, ILogger<SilaDeviceMonitor> logger)
  {
    _device = device;
    _logger = logger;
    _tokenSource = new CancellationTokenSource();
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string DeviceId => _device.UniqueId;

  public void Dispose()
  {
    _tokenSource.Cancel();
    _monitorTask.ContinueWith(_ => _tokenSource.Dispose());
  }

  private Task Monitor(CancellationToken token)
  {
    _logger.LogInformation("Started monitoring SiLA device {DeviceName}", _device.Name);
    return Task.Run(async () =>
    {
      while(!token.IsCancellationRequested)
      {
        var status = _device.Status;

        if(_lastState == OperationalState.Active && status.OperationalState != OperationalState.Active)
        {
          _logger.LogWarning("Lost connection with SiLA device {DeviceName}", _device.Name);
        }

        if(_lastState != OperationalState.Active && status.OperationalState == OperationalState.Active)
        {
          _logger.LogInformation("SiLA device {DeviceName} reconnected, fetching state", _device.Name);

          var state = await _device.GetState();
          _logger.LogInformation("Fetched state for SiLA device {DeviceName} with {FieldCount} fields", _device.Name, state.Fields.Count);
        }

        _lastState = status.OperationalState;

        try
        {
          var tempSource = new CancellationTokenSource();
          var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(tempSource.Token, token);
          var statusTask = _device.StateStream.Select(_ => true).ToTask(combinedSource.Token);
          var delayTask = Task.Delay(TimeSpan.FromSeconds(5), combinedSource.Token);
          _ = await Task.WhenAny(statusTask, delayTask);
          tempSource.Cancel();
        }
        catch(OperationCanceledException ex)
        {
          _logger.LogWarning("SiLA monitoring operation was cancelled for device {DeviceName}. Error Message: {exMessage}", _device.Name, ex.Message);
        }
      }
    }, token);
  }
}
