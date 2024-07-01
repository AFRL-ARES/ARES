using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
/// <summary>
/// Motor target position (−2,147,483,648 to +2,147,483,647 = −0x8000 0000 to +0x7FFF FFFF).
/// Units: microsteps
/// </summary>
public class TargetPosition : SerialResponse
{
  public TargetPosition(int position)
  {
    Position = position;
  }

  public int Position { get; set; }
}
