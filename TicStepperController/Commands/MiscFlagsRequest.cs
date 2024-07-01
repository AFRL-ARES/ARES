using TicStepperController.Commands.Responses;
using TicStepperController.Commands.Responses.Parsers;

namespace TicStepperController.Commands;
/// <summary>
/// Additional information about the Tic’s status
/// </summary>
public class MiscFlagsRequest : VariableCommandRequest<MiscFlags>
{
  public MiscFlagsRequest() : base(0x01, 1, new MiscFlagsParser())
  {
  }
}
