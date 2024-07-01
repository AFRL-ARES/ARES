using System.Text;
using Ares.Device.Serial.Commands;

namespace TC0304.Commands;

internal class HoldCommand : SerialCommand
{
  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("H");
}
