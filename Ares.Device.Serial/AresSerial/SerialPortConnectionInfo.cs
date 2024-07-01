using System.IO.Ports;

namespace Ares.Device.Serial;

public class SerialPortConnectionInfo
{
  public SerialPortConnectionInfo
    (
    int baudRate,
    Parity parity,
    int dataBits,
    StopBits stopBits
    )
  {
    BaudRate = baudRate;
    Parity = parity;
    DataBits = dataBits;
    StopBits = stopBits;
  }

  public int BaudRate { get; set; }
  public Parity Parity { get; set; }
  public int DataBits { get; set; }
  public StopBits StopBits { get; set; }
  public string EndOfInput { get; set; } = string.Empty;
}