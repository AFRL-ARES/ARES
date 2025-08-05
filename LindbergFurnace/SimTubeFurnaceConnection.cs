using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using LindbergFurnace.Commands;
using System.Text;

namespace LindbergFurnace;

public class SimTubeFurnaceConnection : AresSerialSimConnection, ITubeFurnaceConnection
{
  public SimTubeFurnaceConnection(string portName) : base(new SerialPortConnectionInfo(
      9600,
      System.IO.Ports.Parity.None,
      8,
      System.IO.Ports.StopBits.One), portName)
  {

  }

  public override void SendInternally(byte[] bytes)
  {
    var requestStr = Encoding.UTF8.GetString(bytes);
    var responseStart = requestStr.Substring(1, 4);
    var randomTemp = (new Random().Next(1000) + 20) % 1000;
    var responseBody = $"{responseStart}{4:x2}{randomTemp:X4}";
    var lrc = $"{TubeFurnaceCommandHelper.Lrc(responseBody.Select(c => (byte)c)):X2}";
    var response = $":{responseBody}{lrc}\r\n";
    var responseBytes = Encoding.UTF8.GetBytes(response);
    AddDataReceived(responseBytes);
  }
}
