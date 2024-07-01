using Ares.Device.Serial.Commands;

namespace HerkulexDRS.Commands;
internal class PistonDownCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x0C, 0x01, 0x05, 0x76, 0x88, 0x4E, 0x02, 0x00, 0x01, 0x32 };
  }
}
