using Chiller.Services;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.LaserChiller;

public class LaserChillerUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly ChillerRpc.ChillerRpcClient _client;

  public LaserChillerUnitControlViewModel(string deviceId, string deviceName, ChillerRpc.ChillerRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
    ViewType = typeof(LaserChillerUnitControlViewModel);
  }

  public async Task SetChillerTemperature()
  {
    var request = new SetChillerTemperatureRequest() { ChillerId = DeviceId, DesiredTemperature = DesiredTemperature };
    await _client.SetChillerTemperatureAsync(request);
  }

  public async Task GetManifoldTemperature()
  {
    var request = new ChillerRequest() { ChillerId = DeviceId };
    var response = await _client.GetManifoldTemperatureAsync(request);
    DesiredTemperature = response.ManifoldTemperature;
  }

  public async Task SetChillerToStandbyMode()
  {
    var request = new ChillerRequest() { ChillerId = DeviceId };
    await _client.SetChillerStandbyModeAsync(request);
  }

  public async Task SetChillerToRunningMode()
  {
    var request = new ChillerRequest() { ChillerId = DeviceId };
    await _client.SetChillerRunModeAsync(request);
  }

  [Reactive]
  public double CurrentManifoldTemperature { get; set; }
  public double DesiredTemperature { get; set; }
}
