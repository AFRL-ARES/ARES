using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
public class ErrorsOccurredRequest : VariableCommandRequest<ErrorsOccurred>
{
  public ErrorsOccurredRequest() : base(0x04, 4, new ErrorsOccurredParser())
  {
  }
}
