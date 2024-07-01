using System.IO.Ports;
using Ares.Device.Serial;

namespace TC0304;

public class DataloggerThermometerConnection : AresHardwareConnection, IDataloggerThermometerConnection
{
  public DataloggerThermometerConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {
  }
}
