namespace TicStepperController.Commands;
/// <summary>
/// This command sets the target position of the Tic, in microsteps.
/// 
/// Range: −2,147,483,648 to +2,147,483,647 = −0x8000 0000 to +0x7FFF FFFF
/// Units: microsteps
/// </summary>
public class SetTargetPositionCommand : Int32WriteCommand
{
  public SetTargetPositionCommand(int value) : base(0xE0, value)
  {
  }
}
