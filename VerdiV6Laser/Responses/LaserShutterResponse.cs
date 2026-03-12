using Ares.Toolkit.Serial.Commands;

namespace VerdiV6Laser.Responses;

public class LaserShutterResponse : SerialResponse
{
  public LaserShutterResponse(bool shutter)
  {
    Shutter = shutter;
  }

  public bool Shutter { get; }
}
