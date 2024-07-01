using System.IO.Ports;
using Ares.Device.Serial;

namespace LindbergFurnace;

public class TubeFurnaceConnection : AresHardwareConnection, ITubeFurnaceConnection
{
  public TubeFurnaceConnection(string portName) : base(
    new SerialPortConnectionInfo(
      9600,
      Parity.Even,
      7,
      StopBits.One
    ),
    portName
  )
  {
  }

  public bool ReserveAddress(int address)
    => throw new NotImplementedException();

  public void ReleaseAddress(int address)
  {
    throw new NotImplementedException();
  }
}
