using Ares.Toolkit.Serial.Commands;

namespace HerkulexDRS.Commands;
internal class RebootCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x07, 0x01, 0x09, 0x0E, 0xF0 };
  }
}
