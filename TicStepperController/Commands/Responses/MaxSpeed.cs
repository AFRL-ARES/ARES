using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;

/// <summary>
/// Maximum allowed motor speed (0 to 500,000,000).
/// Units: microsteps per 10,000 s
/// </summary>
public class MaxSpeed : SerialResponse
{
  public MaxSpeed(uint speed)
  {
    Speed = speed;
  }

  public uint Speed { get; }
}
