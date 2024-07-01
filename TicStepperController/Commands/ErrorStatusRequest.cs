using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
/// <summary>
/// Indicates the errors that are currently stopping the motor. 
/// The motor can only be controlled normally when there are no errors indicated.
/// </summary>
public class ErrorStatusRequest : VariableCommandRequest<ErrorStatus>
{
  public ErrorStatusRequest() : base(0x02, 2, new ErrorStatusParser())
  {
  }
}
