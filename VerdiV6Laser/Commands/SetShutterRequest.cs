using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Commands;

public class SetShutterRequest : SerialCommand
{
  private readonly bool _shutter;
  public SetShutterRequest(bool shutter)
  {
    _shutter = shutter;
  }

  protected override byte[] Serialize()
  {
    var stringToSerialize = $"S={(_shutter ? "1" : "0")}\r\n";
    return Encoding.ASCII.GetBytes(stringToSerialize);
  }
}
