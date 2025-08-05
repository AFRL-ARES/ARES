using Chiller.Services;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.LaserChiller
{
  public class LaserChillerUnitControlViewModel : SerialDeviceUnitViewModel
  {
    private readonly ChillerRpc.ChillerRpcClient _client;

    public LaserChillerUnitControlViewModel(string deviceName, ChillerRpc.ChillerRpcClient client) : base(deviceName)
    {
      _client = client;
    }

    public async Task SetChillerTemperature()
    {
      var request = new SetChillerTemperatureRequest() { ChillerName = DeviceName, DesiredTemperature = DesiredTemperature };
      await _client.SetChillerTemperatureAsync(request);
    }

    public async Task GetManifoldTemperature()
    {
      var request = new ChillerRequest() { ChillerName = DeviceName };
      var response = await _client.GetManifoldTemperatureAsync(request);
      DesiredTemperature = response.ManifoldTemperature;
    }

    public async Task SetChillerToStandbyMode()
    {
      var request = new ChillerRequest() { ChillerName = DeviceName };
      await _client.SetChillerStandbyModeAsync(request);
    }

    public async Task SetChillerToRunningMode()
    {
      var request = new ChillerRequest() { ChillerName = DeviceName };
      await _client.SetChillerRunModeAsync(request);
    }

    [Reactive]
    public double CurrentManifoldTemperature { get; set; }
    public double DesiredTemperature { get; set; }
  }
}
