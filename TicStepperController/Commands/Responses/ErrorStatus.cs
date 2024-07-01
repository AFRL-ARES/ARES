using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
public class ErrorStatus : SerialResponse
{
  public bool IntentionallyDeEnergized { get; init; }
  public bool MotorDriverError { get; init; }
  public bool LowVin { get; init; }
  public bool KillSwitchActive { get; init; }
  public bool RequiredInputInvalid { get; init; }
  public bool SerialError { get; init; }
  public bool CommandTimeout { get; init; }
  public bool SafeStartViolation { get; init; }
  public bool ErrLineHigh { get; init; }
}
