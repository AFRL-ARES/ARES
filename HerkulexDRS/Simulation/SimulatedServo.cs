using System.Diagnostics;

namespace HerkulexDRS.Simulation;
public class SimulatedServo
{
  private readonly Action<byte[]> _byteSender;

  public SimulatedServo(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public void SendCommand(byte[] command)
  {
    if(command[6] == 0x88)
    {
      Debug.WriteLine("Received Piston Down Command");
    }

    else
    {
      Debug.WriteLine("Received Piston Up Command");
    }
  }
}
