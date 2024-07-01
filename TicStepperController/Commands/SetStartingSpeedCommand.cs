namespace TicStepperController.Commands;
/// <summary>
/// This command temporarily sets the Tic’s starting speed in units of steps per 10,000 seconds.
/// This is the maximum speed at which instant acceleration and deceleration are allowed. 
/// The provided value will override the corresponding setting from the Tic’s non-volatile memory until
/// the next Reset (or Reinitialize) command or full microcontroller reset.
/// 
/// Range: 0 to 500,000,000
/// Units: microsteps per 10,000 s
/// </summary>
public class SetStartingSpeedCommand : Int32WriteCommand
{
  public SetStartingSpeedCommand(uint value) : base(0xE5, (int)value)
  {
  }
}
