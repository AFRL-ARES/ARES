namespace TicStepperController.Commands.Responses.Parsers;
internal class ErrorStatusParser : VariableParser<ErrorStatus>
{
  private byte _intentionallyDeEnergized = 0b0000_0001;
  private byte _motorDriverError = 0b0000_0010;
  private byte _lowVin = 0b0000_0100;
  private byte _killSwitchActive = 0b0000_1000;
  private byte _requiredInputInvalid = 0b0001_0000;
  private byte _serialError = 0b0010_0000;
  private byte _commandTimeout = 0b0100_0000;
  private byte _safeStartViolation = 0b1000_0000;
  private byte _errLineHigh = 0b0000_0001;

  public ErrorStatusParser() : base(2)
  {
  }

  protected override ErrorStatus ParseResponse(byte[] buffer)
  {
    var firstNum = buffer[0];
    var secondNum = buffer[1];
    return new ErrorStatus
    {
      IntentionallyDeEnergized = (firstNum & _intentionallyDeEnergized) == _intentionallyDeEnergized,
      MotorDriverError = (firstNum & _motorDriverError) == _motorDriverError,
      LowVin = (firstNum & _lowVin) == _lowVin,
      KillSwitchActive = (firstNum & _killSwitchActive) == _killSwitchActive,
      RequiredInputInvalid = (firstNum & _requiredInputInvalid) == _requiredInputInvalid,
      SerialError = (firstNum & _serialError) == _serialError,
      CommandTimeout = (firstNum & _commandTimeout) == _commandTimeout,
      SafeStartViolation = (firstNum & _safeStartViolation) == _safeStartViolation,
      ErrLineHigh = (secondNum & _errLineHigh) == _errLineHigh,
    };
  }
}
