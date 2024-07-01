using Ares.Device.Serial;
using System.IO.Ports;

namespace HerkulexDRS;
public class ServoConnection : AresHardwareConnection, IServoConnection
{
  public ServoConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {
  }
}
