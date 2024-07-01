using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class MaxDecelerationRequest : VariableCommandRequest<MaxDeceleration>
{
  public MaxDecelerationRequest() : base(0x1A, 4, new MaxDecelerationParser())
  {
  }
}
