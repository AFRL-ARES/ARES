namespace TicStepperController.Commands.Responses.Parsers;
public class ErrorsOccurredParser : VariableParser<ErrorsOccurred>
{
  private byte _serialFraming = 0b0001;
  private byte _serialRxOverrun = 0b0010;
  private byte _serialFormat = 0b0100;
  private byte _serialCrc = 0b1000;
  private byte _encoderSkip = 0b1_0000;
  public ErrorsOccurredParser() : base(4)
  {
  }

  protected override ErrorsOccurred ParseResponse(byte[] buffer)
  {
    var val = buffer[2];
    return new ErrorsOccurred
    {
      SerialFraming = (val & _serialFraming) == _serialFraming,
      SerialRxOverrun = (val & _serialRxOverrun) == _serialRxOverrun,
      SerialFormat = (val & _serialFormat) == _serialFormat,
      SerialCrc = (val & _serialCrc) == _serialCrc,
      EncoderSkip = (val & _encoderSkip) == _encoderSkip
    };
  }
}
