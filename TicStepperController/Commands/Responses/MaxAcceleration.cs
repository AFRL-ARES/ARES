using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;

/// <summary>
/// Maximum allowed motor acceleration (100 to 2,147,483,647 = 0x64 to 0x7FFF FFFF).
/// Units: microsteps per 100 s²
/// </summary>
public class MaxAcceleration : SerialResponse
{
  public MaxAcceleration(uint acceleration)
  {
    Acceleration = acceleration;
  }

  public uint Acceleration { get; }
}
