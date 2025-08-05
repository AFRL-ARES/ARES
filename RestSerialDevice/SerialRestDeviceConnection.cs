using Ares.Device.Serial;
using System.IO.Ports;

namespace RestSerialDevice;

public class SerialRestDeviceConnection : AresHardwareConnection, ISerialRestDeviceConnection
{
  public SerialRestDeviceConnection(string portName) : base(new SerialPortConnectionInfo(115200, Parity.None, 8, StopBits.One), portName)
  {
  }
  
}
