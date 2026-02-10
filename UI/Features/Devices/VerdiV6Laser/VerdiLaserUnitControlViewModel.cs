using DynamicData.Binding;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.Shared;
using VerdiV6.Services;

namespace UI.Features.Devices.VerdiV6Laser;

public partial class VerdiLaserUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly VerdiV6Rpc.VerdiV6RpcClient _client;
  public VerdiLaserUnitControlViewModel(string id, string deviceName, VerdiV6Rpc.VerdiV6RpcClient client) : base(id, deviceName)
  {
    _client = client;
    this.WhenPropertyChanged(t => t.LaserOn).Subscribe(_ => LaserToggleClicked());
    ViewType = typeof(VerdiLaserControlWidgetView);
  }

  public async Task SetLaserPower()
  {
    IsSavingPowerLevel = true;
    await _client.SetLaserPowerAsync(new SetLaserPowerRequest { DeviceId = DeviceId, LaserPower = DesiredLaserPower });
    IsSavingPowerLevel = false;
  }

  public async Task SetLaserShutter()
  {
    await _client.SetLaserShutterAsync(new SetShutterRequest { DeviceId = DeviceId, Shutter = IsLaserShutterOn });
  }

  public async Task<double> GetLaserPower()
  {
    var getPowerResponse = await _client.GetLaserPowerAsync(new DeviceRequest() { DeviceId = DeviceId });
    return getPowerResponse.LaserPower;
  }

  public async Task<bool> GetLaserShutter()
  {
    var getLaserShutter = await _client.GetLaserShutterAsync(new DeviceRequest() { DeviceId = DeviceId });
    return getLaserShutter.Shutter;
  }

  public void LaserToggleClicked()
  {
    if(LaserOn)
      _client.ActivateLaser(new DeviceRequest() { DeviceId = DeviceId });

    else
      _client.DeactivateLaser(new DeviceRequest() { DeviceId = DeviceId });

  }

  public double DesiredLaserPower { get; set; }

  public bool IsSavingPowerLevel { get; set; } = false;

  public bool IsLaserShutterOn { get; set; }

  [Reactive]
  public partial bool LaserOn { get; set; }
}
