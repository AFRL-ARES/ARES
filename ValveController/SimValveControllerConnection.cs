using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace ValveController;
public class SimValveControllerConnection : AresSimConnection, IValveControllerConnection
{
  private readonly SimulatedValveController _valveController;

  public SimValveControllerConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName)
  {
    _valveController = new SimulatedValveController(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _valveController.SendCommand(bytes);
  }
}
