using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace ValveController.Connection;

public class ValveControllerConnection : AresHardwareConnection, IValveControllerConnection
{
  public ValveControllerConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName,
    new SerialConnectionOptions
    {
      SendBuffer = TimeSpan.FromMilliseconds(50),
      SendTimeout = TimeSpan.FromSeconds(2)
    }
  )
  {
  }
}
