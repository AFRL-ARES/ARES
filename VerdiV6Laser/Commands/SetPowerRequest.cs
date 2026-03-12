using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Commands;

public class SetPowerRequest : SerialCommand
{
  private readonly double _desiredPower;

  public SetPowerRequest(double power)
  {
    _desiredPower = power;
  }

  protected override byte[] Serialize()
  {
    var stringToSerialize = $"P={_desiredPower:F2}\r\n";
    return Encoding.ASCII.GetBytes(stringToSerialize);
  }
}
