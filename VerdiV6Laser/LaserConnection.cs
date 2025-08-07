using Ares.Device.Serial;
using System.IO.Ports;

namespace VerdiV6Laser
{
  public class LaserConnection : AresHardwareConnection, ILaserConnection
  {
    public LaserConnection(string portName) : base(new SerialPortConnectionInfo(19200, System.IO.Ports.Parity.None, 8, StopBits.One), portName)
    {

    }
  }
}
