using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerdiV6Laser.Commands.Responses
{
  internal class LaserShutterResponse : CommandResponse
  {
    public LaserShutterResponse() { }

    public bool Shutter { get; set; }
  }
}
