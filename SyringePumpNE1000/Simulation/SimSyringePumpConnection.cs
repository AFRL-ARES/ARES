using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace SyringePumpNE1000.Simulation;

public class SimSyringePumpConnection : AresSerialSimConnection, ISyringePumpConnection
{
  private readonly SimSyringePump _syringePump;

  public SimSyringePumpConnection(string portName, string deviceName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.None), portName)
  {
    _syringePump = new SimSyringePump(AddDataReceived, deviceName);
  }

  public override void SendInternally(byte[] bytes)
  {
    _syringePump.SendCommand(bytes);
  }
}
