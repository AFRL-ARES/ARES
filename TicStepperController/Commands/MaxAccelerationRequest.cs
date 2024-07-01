using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class MaxAccelerationRequest : VariableCommandRequest<MaxAcceleration>
{
  public MaxAccelerationRequest() : base(0x1E, 4, new MaxAccelerationParser())
  {

  }
}
