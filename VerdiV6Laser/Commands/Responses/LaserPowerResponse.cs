using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerdiV6Laser.Commands.Responses
{
  internal class LaserPowerResponse : CommandResponse
  {
    public LaserPowerResponse(double power) : base()
    {
      Power = power;
    }

    public double Power { get; set; }
  }
}
