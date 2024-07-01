using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
public class MiscFlags : SerialResponse
{
  public bool Energized { get; init; }
  public bool PositionUncertain { get; init; }
  public bool ForwardLimitActive { get; init; }
  public bool ReverseLimitActive { get; init; }
  public bool HomingActive { get; init; }
}
