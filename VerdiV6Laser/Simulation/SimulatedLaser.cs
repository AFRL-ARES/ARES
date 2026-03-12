using System.Text;

namespace VerdiV6Laser.Simulation;

public class SimulatedLaser
{
  private readonly Action<byte[]> _byteSender;

  public SimulatedLaser(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public void SendCommand(byte[] data)
  {
    var stringData = Encoding.ASCII.GetString(data);
    
    if (stringData.StartsWith("?SP"))
    {
      // Get Power Request
      var response = $"{Power:F2}\r\n";
      _byteSender(Encoding.ASCII.GetBytes(response));
    }
    else if (stringData.StartsWith("?S"))
    {
      // Get Shutter Request
      var response = $"{(Shutter ? "1" : "0")}\r\n";
      _byteSender(Encoding.ASCII.GetBytes(response));
    }
    else if (stringData.StartsWith("P="))
    {
      // Set Power Request
      var parts = stringData.Split('=');
      if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out var power))
      {
        Power = power;
      }
    }
    else if (stringData.StartsWith("S="))
    {
      // Set Shutter Request
      Shutter = stringData.Contains('1');
    }
  }

  public double Power { get; set; } = 0.01;
  public bool Shutter { get; set; } = false;
}
