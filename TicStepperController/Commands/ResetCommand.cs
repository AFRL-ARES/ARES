namespace TicStepperController.Commands;
/// <summary>
/// This command makes the Tic forget most parts of its current state. Specifically, it does the following:
///   Reloads all settings from the Tic’s non-volatile memory and discards any temporary changes to the settings previously made with serial commands(this applies to the step mode, current limit, decay mode, max speed, starting speed, max acceleration, and max deceleration settings)
///   Abruptly halts the motor
///   Resets the motor driver
///   Sets the Tic’s operation state to “reset”
///   Clears the last movement command and the current position
///   Clears the encoder position
///   Clears the serial and “command timeout” errors and the “errors occurred” bits
///   Enters safe start if configured to do so
/// </summary>
public class ResetCommand : QuickCommand
{
  public ResetCommand() : base(0xB0)
  {
  }
}
