using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace HerkulexDRS.Connection;
public class ServoConnection : AresHardwareConnection, IServoConnection
{
  public ServoConnection(string portName) : base(new SerialPortConnectionInfo(115200, Parity.None, 8, StopBits.One), portName,
    new SerialConnectionOptions
    {
      SendBuffer = TimeSpan.FromMilliseconds(50),
      SendTimeout = TimeSpan.FromSeconds(2)
    }
  )
  {
  }
}
