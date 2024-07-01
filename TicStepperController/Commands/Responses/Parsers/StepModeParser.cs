using TicStepperController.Commands.Enums;

namespace TicStepperController.Commands.Responses.Parsers;
public class StepModeParser : VariableParser<StepModeResponse>
{
  public StepModeParser() : base(1)
  {
  }

  protected override StepModeResponse ParseResponse(byte[] buffer)
  {
    return new StepModeResponse((StepMode)buffer[0]);
  }
}
