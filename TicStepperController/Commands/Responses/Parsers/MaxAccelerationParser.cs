namespace TicStepperController.Commands.Responses.Parsers;
public class MaxAccelerationParser : VariableParser<MaxAcceleration>
{
  public MaxAccelerationParser() : base(4)
  {
  }

  protected override MaxAcceleration ParseResponse(byte[] buffer)
  {
    return new MaxAcceleration((uint)buffer.ToInt32());
  }
}
