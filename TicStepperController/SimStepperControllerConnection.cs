using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System.Diagnostics;

namespace TicStepperController;
public class SimStepperControllerConnection : AresSerialSimConnection, IStepperControllerConnection
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

    if(bytes.Length == 1)
    {
      if(bytes.First() == 0x83)
        Debug.WriteLine("Exiting Safe Start Mode");

      else
        Debug.WriteLine("Entering Safe Start Mode");

      return;
    }

    var cmd = bytes[1];

    if(cmd == 0x49)
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
