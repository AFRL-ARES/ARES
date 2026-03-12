using Ares.Toolkit.Device.UI;
using ReactiveUI;
using System.Reactive;

namespace ValveController.UI;

public partial class ValveControllerUnitControlViewModel : DeviceUnitControlViewModel<ValveControllerDevice>
{
  public ValveControllerUnitControlViewModel(ValveControllerDevice device) : base(device)
  {
    ViewType = typeof(ValveControllerUnitControl);
    DefaultWidth = 18;

    EngageRelayOneCommand = ReactiveCommand.CreateFromTask(Device.EngageRelayOne);
    DisengageRelayOneCommand = ReactiveCommand.CreateFromTask(Device.DisengageRelayOne);
    EngageRelayTwoCommand = ReactiveCommand.CreateFromTask(Device.EngageRelayTwo);
    DisengageRelayTwoCommand = ReactiveCommand.CreateFromTask(Device.DisengageRelayTwo);
  }

  public ReactiveCommand<Unit, Unit> EngageRelayOneCommand { get; }
  public ReactiveCommand<Unit, Unit> DisengageRelayOneCommand { get; }
  public ReactiveCommand<Unit, Unit> EngageRelayTwoCommand { get; }
  public ReactiveCommand<Unit, Unit> DisengageRelayTwoCommand { get; }
}
