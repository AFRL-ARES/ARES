using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses;
/// <summary>
/// Maximum speed at which instant acceleration and deceleration are allowed (0 to 500,000,000).
/// Units: microsteps per 10,000 s
/// </summary>
public class StartingSpeed : SerialResponse
{
  public StartingSpeed(uint startingSpeed)
  {
    Speed = startingSpeed;
  }

  public uint Speed { get; }
}
