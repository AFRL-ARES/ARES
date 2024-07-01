using Ares.Device.Serial.Commands;
using TicStepperController.Commands.Enums;

namespace TicStepperController.Commands.Responses;
public class OperationStateResponse : SerialResponse
{
  public OperationStateResponse(OperationState state)
  {
    State = state;
  }

  public OperationState State { get; }
}
