using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;

public class CurrentLimitRequest : VariableCommandRequest<CurrentLimit>
{
  public CurrentLimitRequest() : base(0x4A, 4, new CurrentLimitParser())
  {
  }
}
