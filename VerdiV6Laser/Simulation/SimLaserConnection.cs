using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;

namespace VerdiV6Laser.Simulation;

public class SimLaserConnection : AresSerialSimConnection, ILaserConnection
{
  private readonly SimulatedLaser _laser;

  public SimLaserConnection(string portName) 
    : base(new SerialPortConnectionInfo(19200, Parity.None, 8, StopBits.One), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(50) })
  {
    _laser = new SimulatedLaser(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _laser.SendCommand(bytes);
  }
}
