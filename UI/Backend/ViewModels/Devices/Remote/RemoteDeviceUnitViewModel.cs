using Ares.Datamodel;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceUnitViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private readonly AresDevices.AresDevicesClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public RemoteDeviceUnitViewModel(string id, string name, AresDevices.AresDevicesClient client) : base(id, name)
  {
    _client = client;
    _ = StartStateUpdater();
  }

  private async Task StartStateUpdater()
  {
    using var call = _client.GetDeviceStateStream(new DeviceStateStreamRequest { DeviceId = DeviceId, IntervalMs = 500 });
    await foreach(var msg in call.ResponseStream.ReadAllAsync(_stateUpdateTokenSource.Token))
    {
      DeviceState = msg.State;
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
    await _stateListener;
    _stateListener.Dispose();
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }
}
