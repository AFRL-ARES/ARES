using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace VerdiV6Laser;

public class LaserConnection : AresHardwareConnection, ILaserConnection
{
  public LaserConnection(string portName) : base(new SerialPortConnectionInfo(19200, Parity.None, 8, StopBits.One), portName,
    new SerialConnectionOptions
    {
      SendBuffer = TimeSpan.FromMilliseconds(50),
      SendTimeout = TimeSpan.FromSeconds(2)
    }
  )
  {
  }
}
