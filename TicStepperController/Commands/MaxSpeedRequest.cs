using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class MaxSpeedRequest : VariableCommandRequest<MaxSpeed>
{
  public MaxSpeedRequest() : base(0x16, 4, new MaxSpeedParser())
  {
  }
}
