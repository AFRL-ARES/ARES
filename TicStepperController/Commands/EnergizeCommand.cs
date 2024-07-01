namespace TicStepperController.Commands;
/// <summary>
/// This command is a request for the Tic to energize the stepper motor coils 
/// by enabling its stepper motor driver. The Tic will clear the 
/// “intentionally de-energized” error bit. If there are no other errors, 
/// this allows the system to start up.
/// </summary>
public class EnergizeCommand : QuickCommand
{
  public EnergizeCommand() : base(0x85)
  {
  }
}
