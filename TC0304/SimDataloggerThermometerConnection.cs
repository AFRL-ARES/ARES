using System.IO.Ports;
using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;

namespace TC0304;

public class SimDataloggerThermometerConnection : AresSimConnection, IDataloggerThermometerConnection
{
  private readonly SimulatedDataLogger _dataLogger;

  public SimDataloggerThermometerConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName)
  {
    _dataLogger = new SimulatedDataLogger(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _dataLogger.SendCommand(bytes);
  }
}
