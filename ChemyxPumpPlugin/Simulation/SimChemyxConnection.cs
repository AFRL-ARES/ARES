using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;

namespace ChemyxPumpPlugin.Simulation;

public class SimChemyxConnection(string portName) : AresSerialSimConnection(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
{
  private SimChemyxPump? _pump;

  public override void SendInternally(byte[] bytes)
  {
    _pump ??= new SimChemyxPump(AddDataReceived);
    _pump.SendCommand(bytes);
  }
}
