using TicStepperController.Commands.Enums;

namespace TicStepperController.Commands.Responses.Parsers;
public class OperationStateParser : VariableParser<OperationStateResponse>
{
  public OperationStateParser() : base(1)
  {
  }

  protected override OperationStateResponse ParseResponse(byte[] buffer)
  {
    var state = (OperationState)buffer[0];
    return new OperationStateResponse(state);
  }
}
