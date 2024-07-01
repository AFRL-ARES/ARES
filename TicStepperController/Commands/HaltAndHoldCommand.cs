namespace TicStepperController.Commands;
/// <summary>
/// This command stops the motor abruptly without respecting the deceleration limit.
/// Besides stopping the motor, this command also sets the “position uncertain” flag 
/// (because the abrupt stop might cause steps to be missed), sets the input state to “halt”, 
/// and clears the “input after scaling” variable.
/// </summary>
public class HaltAndHoldCommand : QuickCommand
{
  public HaltAndHoldCommand() : base(0x89)
  {
  }
}
