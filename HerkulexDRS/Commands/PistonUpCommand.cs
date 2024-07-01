using Ares.Device.Serial.Commands;

namespace HerkulexDRS.Commands;
internal class PistonUpCommand : SerialCommand
{
  public PistonUpCommand()
  {

  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x0C, 0x01, 0x05, 0xDA, 0x24, 0xE0, 0x01, 0x00, 0x01, 0x32 };
  }
}
