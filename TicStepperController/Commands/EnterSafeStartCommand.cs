namespace TicStepperController.Commands;
/// <summary>
/// If safe start is enabled and the control mode is Serial / I²C / USB, RC speed, analog speed, or encoder speed, this command causes the Tic to stop the motor (using the configured soft error response behavior) and set its “safe start violation” error bit. If safe start is disabled, or if the Tic is not in one of the listed modes, this command will cause a brief interruption in motor control (during which the soft error response behavior will be triggered) but otherwise have no effect.
/// </summary>
public class EnterSafeStartCommand : QuickCommand
{
  public EnterSafeStartCommand() : base(0x8F)
  {
  }
}
