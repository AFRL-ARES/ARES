namespace TicStepperController.Commands.Responses.Parsers;
public class MaxDecelerationParser : VariableParser<MaxDeceleration>
{
  public MaxDecelerationParser() : base(4)
  {
  }

  protected override MaxDeceleration ParseResponse(byte[] buffer)
  {
    return new MaxDeceleration((uint)buffer.ToInt32());
  }
}
