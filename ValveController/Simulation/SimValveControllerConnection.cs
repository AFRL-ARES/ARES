using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using ValveController.Connection;
using System.IO.Ports;

namespace ValveController.Simulation;

public class SimValveControllerConnection : AresSerialSimConnection, IValveControllerConnection
{
  private readonly SimulatedValveController _valveController;

  public SimValveControllerConnection(string portName) 
    : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(50) })
  {
    _valveController = new SimulatedValveController(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _valveController.SendCommand(bytes);
  }
}
