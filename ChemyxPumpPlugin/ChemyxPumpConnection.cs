using Ares.Device.Serial;
using System.IO.Ports;

namespace ChemyxPumpPlugin;

public class ChemyxPumpConnection : AresHardwareConnection, IChemyxPumpConnection
{
  static readonly SerialPortConnectionInfo _connectionInfo = new(
      19200,
      Parity.None,
      8,
      StopBits.One
    );

  static readonly SerialConnectionOptions _options = new()
  {
    SendBuffer = TimeSpan.FromMilliseconds(50),
    SendTimeout = TimeSpan.FromSeconds(2)
  };

  public ChemyxPumpConnection(SerialPortConnectionInfo connectionInfo, string portName, SerialConnectionOptions? connectionOptions = null) : base(
    _connectionInfo,
    portName,
    _options)
  { 
  }
}
