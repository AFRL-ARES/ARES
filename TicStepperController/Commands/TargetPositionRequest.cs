using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class TargetPositionRequest : VariableCommandRequest<TargetPosition>
{
  public TargetPositionRequest() : base(0x0A, 4, new TargetPositionParser())
  {
  }
}
