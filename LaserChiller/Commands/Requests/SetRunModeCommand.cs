using Ares.Device.Serial.Commands;

namespace LaserChiller.Commands.Requests;

public class SetRunModeCommand : SerialCommand
{
  public SetRunModeCommand()
  {

  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0x2E, 0x47, 0x31, 0x41, 0x36, 0x0D };
  }
}
