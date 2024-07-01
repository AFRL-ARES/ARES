using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class StepModeRequest : VariableCommandRequest<StepModeResponse>
{
  public StepModeRequest() : base(0x49, 1, new StepModeParser())
  {
  }
}
