namespace TicStepperController.Commands;
/// <summary>
/// This command temporarily sets the Tic’s maximum allowed motor speed in units of steps per 10,000 seconds.
/// The provided value will override the corresponding setting from the Tic’s non-volatile memory 
/// until the next Reset (or Reinitialize) command or full microcontroller reset.
/// 
/// Range: 0 to 500,000,000
/// Units: microsteps per 10,000 s
/// </summary>
public class SetMaxSpeedCommand : Int32WriteCommand
{
  public SetMaxSpeedCommand(uint value) : base(0xE6, (int)value)
  {
  }
}
