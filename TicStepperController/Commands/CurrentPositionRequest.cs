using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class CurrentPositionRequest : VariableCommandRequest<CurrentPosition>
{
  public CurrentPositionRequest() : base(0x22, 4, new CurrentPositionParser())
  {
  }
}
