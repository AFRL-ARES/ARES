namespace TicStepperController.Commands.Responses.Parsers;

public class CurrentLimitParser : VariableParser<CurrentLimit>
{
  public CurrentLimitParser() : base(4)
  {
  }

  protected override CurrentLimit ParseResponse(byte[] buffer)
  {
    return new CurrentLimit(buffer[0]);
  }
}
