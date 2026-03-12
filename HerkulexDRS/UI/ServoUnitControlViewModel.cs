using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;

namespace HerkulexDRS.UI;

public partial class ServoUnitControlViewModel : DeviceUnitControlViewModel<Servo>
{
  private readonly ILogger<ServoUnitControlViewModel> _logger;

  public ServoUnitControlViewModel(Servo servo, ILogger<ServoUnitControlViewModel> logger) : base(servo)
  {
    _logger = logger;

    PistonUpCommand = ReactiveCommand.CreateFromTask(Device.PistonUp);
    PistonDownCommand = ReactiveCommand.CreateFromTask(Device.PistonDown);
    ResetServoCommand = ReactiveCommand.CreateFromTask(Device.ResetServo);
    
    ViewType = typeof(ServoUnitControl);
    DefaultWidth = 20;
  }

  public ReactiveCommand<Unit, Unit> PistonUpCommand { get; }
  public ReactiveCommand<Unit, Unit> PistonDownCommand { get; }
  public ReactiveCommand<Unit, Unit> ResetServoCommand { get; }
}
