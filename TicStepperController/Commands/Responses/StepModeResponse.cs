using Ares.Device.Serial.Commands;
using TicStepperController.Commands.Enums;

namespace TicStepperController.Commands.Responses;
public class StepModeResponse : SerialResponse
{
  public StepModeResponse(StepMode stepMode)
  {
    StepMode = stepMode;
  }

  public StepMode StepMode { get; }
}
