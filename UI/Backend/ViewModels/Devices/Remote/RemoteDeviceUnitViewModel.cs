using Ares.Datamodel;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceUnitViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private readonly AresDevices.AresDevicesClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener;

  public RemoteDeviceUnitViewModel(string id, string name, AresDevices.AresDevicesClient client) : base(id, name)
  {
    _client = client;
    _stateListener = StartStateUpdater();
  }

  private async Task StartStateUpdater()
  {
    try
    {
      using var call = _client.GetDeviceStateStream(new DeviceStateStreamRequest { DeviceId = DeviceId, IntervalMs = 500 });
      await foreach (var msg in call.ResponseStream.ReadAllAsync(_stateUpdateTokenSource.Token))
      {
        DeviceState = msg.State;
      }
    }
    catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
    {
      // Expected cancellation
    }
    catch (OperationCanceledException)
    {
      // Expected cancellation
    }
    catch (RpcException e)
    {
      // Log unexpected gRPC errors. A proper logging framework would be ideal here.
      System.Diagnostics.Debug.WriteLine($"[gRPC Error] State updater for device {DeviceId} failed: {e.Status}");
    }
    catch (Exception e)
    {
      // Catch-all for any other unexpected errors
      System.Diagnostics.Debug.WriteLine($"[Error] State updater for device {DeviceId} failed: {e.Message}");
    }
  }

  private void StopStateUpdater()
  {
    _stateUpdateTokenSource.Cancel();
  }

  private async Task UpdateState()
  {
    var response = await _client.GetDeviceStateAsync(new DeviceStateRequest() { DeviceId = DeviceId });
    DeviceState = response.State;
  }

  [Reactive]
  public AresStruct? DeviceState { get; private set; }

  public async ValueTask DisposeAsync()
  {
    StopStateUpdater();
    await _stateListener.ConfigureAwait(false);
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }
}
