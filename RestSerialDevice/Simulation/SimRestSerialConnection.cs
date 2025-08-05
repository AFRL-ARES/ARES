using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace RestSerialDevice.Simulation;

public class SimRestSerialConnection : AresSerialSimConnection, ISerialRestDeviceConnection
{
  public SimRestSerialConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.None), portName)
  {

  }

  public override void SendInternally(byte[] bytes)
  {
    throw new NotImplementedException();
  }
}
