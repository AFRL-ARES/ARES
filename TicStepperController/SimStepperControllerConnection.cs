using System.Diagnostics;
using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;

namespace TicStepperController;
public class SimStepperControllerConnection : AresSimConnection, IStepperControllerConnection
{
  public SimStepperControllerConnection(string portName) : base(new SerialPortConnectionInfo(
      9600,
      System.IO.Ports.Parity.None,
      8,
      System.IO.Ports.StopBits.One), portName)
  {

  }

  public override void SendInternally(byte[] bytes)
  {
    var byteString = string.Join(" ", bytes.Select(b => $"{b:X2}"));


    var cmd = bytes[1];

    if (cmd == 0x49)
    {
      AddDataReceived(new byte[] { 0x01 });
    }
    else
    {
      AddDataReceived(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
    }

    Debug.WriteLine(byteString);
  }
}
