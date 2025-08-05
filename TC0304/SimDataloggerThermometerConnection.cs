using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;

namespace TC0304;

public class SimDataloggerThermometerConnection : AresSerialSimConnection, IDataloggerThermometerConnection
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
