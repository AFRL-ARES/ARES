namespace TicStepperController.Commands;
/// <summary>
/// If the command timeout is enabled, this command resets it and 
/// prevents the “command timeout” error from happening for some time.
/// </summary>
public class ResetCommandTimeoutCommand : QuickCommand
{
  public ResetCommandTimeoutCommand() : base(0x8C)
  {
  }
}
