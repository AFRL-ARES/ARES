using Ares.Toolkit.Serial.Commands;

namespace VerdiV6Laser.Responses;

public class LaserPowerResponse : SerialResponse
{
  public LaserPowerResponse(double power)
  {
    Power = power;
  }

  public double Power { get; }
}
