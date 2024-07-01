using Ares.Device.Serial;
using System.IO.Ports;

namespace ValveController;
public class ValveControllerConnection : AresHardwareConnection, IValveControllerConnection
{
  public ValveControllerConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {

  }
}
