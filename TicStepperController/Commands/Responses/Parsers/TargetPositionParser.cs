namespace TicStepperController.Commands.Responses.Parsers;
public class TargetPositionParser : VariableParser<TargetPosition>
{
  public TargetPositionParser() : base(4)
  {
  }

  protected override TargetPosition ParseResponse(byte[] buffer)
  {
    return new TargetPosition(buffer.ToInt32());
  }
}
