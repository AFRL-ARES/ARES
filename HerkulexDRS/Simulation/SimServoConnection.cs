using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace HerkulexDRS.Simulation;
public class SimServoConnection : AresSerialSimConnection, IServoConnection
{
  private readonly SimulatedServo _servo;
  public SimServoConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.None), portName)
  {
    _servo = new SimulatedServo(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _servo.SendCommand(bytes);
  }
}
