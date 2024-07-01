using System.Text;
using Ares.Device.Serial.Commands;

namespace TC0304.Commands;

internal class ToggleTemperatureUnitCommand : SerialCommand
{
  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("C");
}
