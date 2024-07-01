using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
/// <summary>
/// Maximum allowed motor deceleration (100 to 2,147,483,647 = 0x64 to 0x7FFF FFFF).
/// Units: microsteps per 100 s²
/// </summary>
public class MaxDeceleration : SerialResponse
{
  public MaxDeceleration(uint deceleration)
  {
    Deceleration = deceleration;
  }

  public uint Deceleration { get; }
}
