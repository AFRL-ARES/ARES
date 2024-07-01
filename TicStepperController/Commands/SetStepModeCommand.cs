using TicStepperController.Commands.Enums;

namespace TicStepperController.Commands;
/// <summary>
/// This command temporarily sets the step mode (also known as microstepping mode)
/// of the driver on the Tic, which defines how many microsteps correspond to one full step.
/// The provided value will override the corresponding setting from the Tic’s non-volatile
/// memory until the next Reset (or Reinitialize) command or full microcontroller reset.
/// </summary>
public class SetStepModeCommand : Int7WriteCommand
{
  public SetStepModeCommand(StepMode value) : base(0x94, (byte)value)
  {
  }
}
