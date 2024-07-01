namespace TicStepperController.Commands.Responses.Parsers;
public class MaxSpeedParser : VariableParser<MaxSpeed>
{
  public MaxSpeedParser() : base(4)
  {
  }

  protected override MaxSpeed ParseResponse(byte[] buffer)
  {
    return new MaxSpeed((uint)buffer.ToInt32());
  }
}
