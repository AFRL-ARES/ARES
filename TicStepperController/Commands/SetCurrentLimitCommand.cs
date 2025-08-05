namespace TicStepperController.Commands;

public class SetCurrentLimitCommand : Int7WriteCommand
{
  public SetCurrentLimitCommand(uint value) : base(0x91, Convert.ToByte(value))
  {

  }
}
