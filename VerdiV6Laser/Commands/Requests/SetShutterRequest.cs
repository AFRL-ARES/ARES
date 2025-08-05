using Ares.Device.Serial.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerdiV6Laser.Commands.Requests
{
  internal class SetShutterRequest : SerialCommand
  {
    private bool _shutter;
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
}
