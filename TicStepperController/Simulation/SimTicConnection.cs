using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.Diagnostics;
using System.IO.Ports;
using TicStepperController.Commands;

namespace TicStepperController.Simulation;

public class SimTicConnection(string portName) : AresSerialSimConnection(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
{
  private int _currentPosition = 0;
  private int _targetPosition = 0;
  private bool _energized = true;
  private uint _maxAcceleration = 10000;
  private uint _maxDeceleration = 10000;
  private uint _maxSpeed = 1000000;
  private uint _startingSpeed = 0;
  private uint _currentLimit = 32;

  public override void SendInternally(byte[] bytes)
  {
    if (bytes.Length == 0) return;

    // Handle Write Commands (Minimal implementation for simulation)
    if (bytes[0] == 0x85) { _energized = true; }
    else if (bytes[0] == 0x86) { _energized = false; }
    else if (bytes[0] == 0xE0 && bytes.Length >= 6) // Set Target Position (5-byte value)
    {
      _targetPosition = bytes[1..6].FromPololuByteArray();
    }
    else if (bytes[0] == 0xEC && bytes.Length >= 6) // Halt and Set Position
    {
      _targetPosition = bytes[1..6].FromPololuByteArray();
      _currentPosition = _targetPosition;
    }
    else if (bytes[0] == 0xEA && bytes.Length >= 6) { _maxAcceleration = (uint)bytes[1..6].FromPololuByteArray(); }
    else if (bytes[0] == 0xE9 && bytes.Length >= 6) { _maxDeceleration = (uint)bytes[1..6].FromPololuByteArray(); }
    else if (bytes[0] == 0xE6 && bytes.Length >= 6) { _maxSpeed = (uint)bytes[1..6].FromPololuByteArray(); }
    else if (bytes[0] == 0xE5 && bytes.Length >= 6) { _startingSpeed = (uint)bytes[1..6].FromPololuByteArray(); }
    else if (bytes[0] == 0x91 && bytes.Length >= 2) { _currentLimit = bytes[1]; }

    if (bytes[0] == 0xA1) // Variable Request
    {
      if (bytes.Length < 3) return;
      var offset = bytes[1];
      var length = bytes[2];

      byte[] response = new byte[length];
      
      switch(offset)
      {
        case 0x00: response[0] = 10; break; // OperationState.Normal
        case 0x01: response[0] = (byte)(_energized ? 1 : 0); break; // Misc Flags (Energized is bit 0)
        case 0x22: // Current Position
          BitConverter.GetBytes(_currentPosition).CopyTo(response, 0);
          break;
        case 0x0A: // Target Position
          BitConverter.GetBytes(_targetPosition).CopyTo(response, 0);
          break;
        case 0x1E: BitConverter.GetBytes(_maxAcceleration).CopyTo(response, 0); break;
        case 0x1A: BitConverter.GetBytes(_maxDeceleration).CopyTo(response, 0); break;
        case 0x16: BitConverter.GetBytes(_maxSpeed).CopyTo(response, 0); break;
        case 0x12: BitConverter.GetBytes(_startingSpeed).CopyTo(response, 0); break;
        case 0x4A: BitConverter.GetBytes(_currentLimit).CopyTo(response, 0); break;
      }
      
      AddDataReceived(response);
    }
    
    // Basic movement simulation
    if (_currentPosition < _targetPosition) _currentPosition += 10;
    else if (_currentPosition > _targetPosition) _currentPosition -= 10;

    Debug.WriteLine($"Simulated Tic received: {BitConverter.ToString(bytes)}");
  }
}
