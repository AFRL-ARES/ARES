using Ares.Device.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Commands.Requests
{
  internal class SetPowerRequest : SerialCommand
  {
    private readonly double _desiredPower;

    public SetPowerRequest(double power)
    {
      _desiredPower = power;
    }

    protected override byte[] Serialize()
    {
      var stringToSerialize = $"P={_desiredPower}\r\n";
      return Encoding.ASCII.GetBytes(stringToSerialize);
    }
  }
}
