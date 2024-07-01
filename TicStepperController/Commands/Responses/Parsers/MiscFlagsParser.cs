namespace TicStepperController.Commands.Responses.Parsers;
internal class MiscFlagsParser : VariableParser<MiscFlags>
{
  private byte _energized = 0b0000_0001;
  private byte _positionUncertain = 0b0000_0010;
  private byte _forwardLimitActive = 0b0000_0100;
  private byte _reverseLimitActive = 0b0000_1000;
  private byte _homingActive = 0b0001_0000;

  public MiscFlagsParser() : base(1)
  {
  }

  protected override MiscFlags ParseResponse(byte[] buffer)
  {
    var val = buffer[0];
    return new MiscFlags
    {
      Energized = (val & _energized) == _energized,
      PositionUncertain = (val & _positionUncertain) == _positionUncertain,
      ForwardLimitActive = (val & _forwardLimitActive) == _forwardLimitActive,
      ReverseLimitActive = (val & _reverseLimitActive) == _reverseLimitActive,
      HomingActive = (val & _homingActive) == _homingActive
    };
  }
}
