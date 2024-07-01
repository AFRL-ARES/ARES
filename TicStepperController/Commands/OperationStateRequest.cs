using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
/// <summary>
/// Overall state of the Tic
/// </summary>
public class OperationStateRequest : VariableCommandRequest<OperationStateResponse>
{
  public OperationStateRequest() : base(0x00, 1, new OperationStateParser())
  {
  }
}
