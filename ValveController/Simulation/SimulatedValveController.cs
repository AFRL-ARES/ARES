using System.Diagnostics;

namespace ValveController.Simulation;

public class SimulatedValveController
{
  private readonly Action<byte[]> _byteSender;
  private bool _relayOneOn = false;
  private bool _relayTwoOn = false;
  private bool _commandMode = false;

  public SimulatedValveController(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public void SendCommand(byte[] command)
  {
    if (command.Length == 0) return;

    byte cmd = command[0];

    // Command mode trigger
    if (cmd == 254)
    {
      _commandMode = true;
      Debug.WriteLine("Simulated Valve Controller: Entered Command Mode");
      return;
    }

    if (!_commandMode)
    {
      Debug.WriteLine($"Simulated Valve Controller: Received {cmd} but not in command mode.");
      return;
    }

    // Reset command mode after every command or keep it? 
    // The real hardware seems to require it before every command based on the implementation.
    _commandMode = false;

    switch (cmd)
    {
      case 0:
        _relayOneOn = false;
        Debug.WriteLine("Simulated Valve Controller: Relay One Disengaged");
        break;
      case 1:
        _relayOneOn = true;
        Debug.WriteLine("Simulated Valve Controller: Relay One Engaged");
        break;
      case 2:
        _relayTwoOn = false;
        Debug.WriteLine("Simulated Valve Controller: Relay Two Disengaged");
        break;
      case 3:
        _relayTwoOn = true;
        Debug.WriteLine("Simulated Valve Controller: Relay Two Engaged");
        break;
      case 7:
        byte status = 0;
        if (_relayOneOn) status |= 1;
        if (_relayTwoOn) status |= 2;
        _byteSender(new byte[] { status });
        Debug.WriteLine($"Simulated Valve Controller: Status Requested. Sending {status}");
        break;
      case 248:
        Debug.WriteLine("Simulated Valve Controller: All Relays Enabled");
        break;
      default:
        Debug.WriteLine($"Simulated Valve Controller: Unknown Command {cmd}");
        break;
    }
  }
}
