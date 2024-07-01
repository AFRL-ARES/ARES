namespace TicStepperController.Commands.Responses.Parsers;
public class StartingSpeedParser : VariableParser<StartingSpeed>
{
  public StartingSpeedParser() : base(4)
  {
  }

  protected override StartingSpeed ParseResponse(byte[] buffer)
  {
    return new StartingSpeed((uint)buffer.ToInt32());
  }
}
