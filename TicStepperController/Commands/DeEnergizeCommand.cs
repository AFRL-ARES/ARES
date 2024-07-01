namespace TicStepperController.Commands;
/// <summary>
/// This command causes the Tic to de-energize the stepper motor coils by disabling its stepper motor driver.
/// The motor will stop moving and consuming power. This command sets the “position uncertain” flag 
/// (because the Tic is no longer in control of the motor’s position); the Tic will also set the 
/// “intentionally de-energized” error bit, turn on its red LED, and drive its ERR line high.
/// </summary>
public class DeEnergizeCommand : QuickCommand
{
  public DeEnergizeCommand() : base(0x86)
  {
  }
}
