using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace VerdiV6Laser.Simulated
{
  public class SimulatedLaserConnection : AresSerialSimConnection, ILaserConnection
  {
    private readonly SimulatedLaser _simulatedLaser;

    public SimulatedLaserConnection(string portName) : base(new SerialPortConnectionInfo(19200, System.IO.Ports.Parity.None, 8, StopBits.One), portName)
    {
      _simulatedLaser = new SimulatedLaser(AddDataReceived);
    }

    public override void SendInternally(byte[] bytes)
    {
      _simulatedLaser.SendCommand(bytes);
    }
  }
}
