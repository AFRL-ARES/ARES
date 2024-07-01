namespace TicStepperController.Commands.Responses.Parsers;
public class CurrentPositionParser : VariableParser<CurrentPosition>
{
  public CurrentPositionParser() : base(4)
  {
  }

  protected override CurrentPosition ParseResponse(byte[] buffer)
  {
    return new CurrentPosition(buffer.ToInt32());
  }
}
