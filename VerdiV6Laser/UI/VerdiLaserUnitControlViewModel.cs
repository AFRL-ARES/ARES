using Ares.Toolkit.Device.UI;
using ReactiveUI;
using System.Reactive;
using ReactiveUI.SourceGenerators;

namespace VerdiV6Laser.UI;

public partial class VerdiLaserUnitControlViewModel : DeviceUnitControlViewModel<VerdiV6LaserDevice>
{
  public VerdiLaserUnitControlViewModel(VerdiV6LaserDevice device) : base(device)
  {
    ViewType = typeof(VerdiLaserControlWidgetView);
    DefaultWidth = 20;
  }

  public async Task SetLaserPower()
  {
    IsSavingPowerLevel = true;
    await Device.SetLaserPower(DesiredLaserPower);
    IsSavingPowerLevel = false;
  }

  [Reactive]
  public partial double DesiredLaserPower { get; set; } = 0.01;

  [Reactive]
  public partial bool LaserOn { get; set; }

  [Reactive]
  public partial bool IsSavingPowerLevel { get; set; } = false;

  [Reactive]
  public partial bool IsLaserShutterOn { get; set; }
}
