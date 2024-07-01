namespace TicStepperController.Commands;
/// <summary>
/// This command temporarily sets the Tic’s maximum allowed motor deceleration 
/// in units of steps per second per 100 seconds.
/// The provided value will override the corresponding setting from the Tic’s non-volatile
/// memory until the next Reset (or Reinitialize) command or full microcontroller reset.
/// 
/// If the provided value is 0, then the max deceleration will be set equal to the current
/// max acceleration value.If the provided value is between 1 and 99, it is treated as 100.
/// 
/// Range: 100 to 2,147,483,647 = 0x64 to 0x7FFF FFFF
/// Units: microsteps per 100 s²
/// </summary>
internal class SetMaxDecelerationCommand : Int32WriteCommand
{
  public SetMaxDecelerationCommand(uint value) : base(0xE9, (int)value)
  {
  }
}
