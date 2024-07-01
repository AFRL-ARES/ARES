using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class StartingSpeedRequest : VariableCommandRequest<StartingSpeed>
{
  public StartingSpeedRequest() : base(0x12, 4, new StartingSpeedParser())
  {
  }
}
