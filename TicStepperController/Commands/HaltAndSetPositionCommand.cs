namespace TicStepperController.Commands;
/// <summary>
/// This command stops the motor abruptly without respecting the deceleration limit and sets the “Current position” variable, which represents what position the Tic currently thinks the motor is in.
/// Besides stopping the motor and setting the current position, this command also clears the “position uncertain” flag, sets the input state to “halt”, and clears the “input after scaling” variable.
/// 
/// Range: −2,147,483,648 to +2,147,483,647 = −0x8000 0000 to +0x7FFF FFFF
/// Units: microsteps
/// </summary>
public class HaltAndSetPositionCommand : Int32WriteCommand
{
  public HaltAndSetPositionCommand(int value) : base(0xEC, value)
  {
  }
}
