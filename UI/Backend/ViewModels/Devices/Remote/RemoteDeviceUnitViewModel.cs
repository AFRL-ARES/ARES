using Ares.Datamodel;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceUnitViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private readonly AresDevices.AresDevicesClient  _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public RemoteDeviceUnitViewModel(string id, string name, AresDevices.AresDevicesClient client) : base(id, name)
  {
    _client = client;
    StartStateUpdater();
  }

  private void StartStateUpdater()
  {
    _stateListener = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = $"Remote Device {DeviceName} State Listener View Model Thread";
      while(!_stateUpdateTokenSource.Token.IsCancellationRequested)
      {
        await UpdateState();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
      }
    },
      _stateUpdateTokenSource.Token,
      TaskCreationOptions.LongRunning);
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
