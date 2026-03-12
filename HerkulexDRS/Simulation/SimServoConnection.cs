using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using HerkulexDRS.Connection;
using System.IO.Ports;

namespace HerkulexDRS.Simulation;
public class SimServoConnection : AresSerialSimConnection, IServoConnection
{
  private readonly SimulatedServo _servo;
  public SimServoConnection(string portName) : base(new SerialPortConnectionInfo(115200, Parity.None, 8, StopBits.One), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
  {
    _servo = new SimulatedServo(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _servo.SendCommand(bytes);
  }
}
