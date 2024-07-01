namespace TicStepperController.Commands;
/// <summary>
/// In Serial / I²C / USB control mode, this command causes the “safe start violation” 
/// error to be cleared for 200 ms. If there are no other errors, 
/// this allows the system to start up.
/// </summary>
public class ExitSafeStartCommand : QuickCommand
{
  public ExitSafeStartCommand() : base(0x83)
  {
  }
}
