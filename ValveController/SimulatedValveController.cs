using System.Diagnostics;

namespace ValveController;
public class SimulatedValveController
{
  private readonly Action<byte[]> _byteSender;

  public SimulatedValveController(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public void SendCommand(byte[] data)
  {
    switch (data[0])
    {
      case 0:
        Debug.WriteLine("Relay One Disengaged");
        return;

      case 1:
        Debug.WriteLine("Relay One Engaged");
        return;

      case 2:
        Debug.WriteLine("Relay Two Disengaged");
        return;

      case 3:
        Debug.WriteLine("Relay Two Engaged");
        return;

      case 7:
        Debug.WriteLine("Received Relay Status Request");
        SendStatusResponse();
        return;

      case 248:
        Debug.WriteLine("All Valve Controller Devices Enabled");
        return;

      case 254:
        Debug.WriteLine("Valve Controller has Entered Command Mode");
        return;

      default:
        Debug.WriteLine("Unknown Command Received");
        break;
    }
  }

  public void SendStatusResponse()
  {
    var response = new byte[] { 0 };
    _byteSender(response);
  }
}
