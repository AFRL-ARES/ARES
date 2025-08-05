using Ares.Device.Serial.Commands;

namespace LaserChiller.Commands.Requests;

public class SetStandbyModeCommand : SerialCommand
{
  public SetStandbyModeCommand()
  {
  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0x2E, 0x47, 0x30, 0x41, 0x35, 0x0D };
  }
}
