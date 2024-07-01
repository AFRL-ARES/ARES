using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
/// <summary>
/// Current position of the motor (−2,147,483,648 to +2,147,483,647 = −0x8000 0000 to +0x7FFF FFFF).
/// Note that this just tracks steps that the Tic has commanded the stepper driver to take;
/// it could be different from the actual position of the motor for various reasons.
/// </summary>
public class CurrentPosition : SerialResponse
{
  public CurrentPosition(int position)
  {
    Position = position;
  }

  public int Position { get; set; }
}
