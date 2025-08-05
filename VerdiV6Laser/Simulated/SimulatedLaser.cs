using System.Text;

namespace VerdiV6Laser.Simulated
{
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
      if(stringData.StartsWith("?SP"))
      {
        //Get Power Request
        Console.WriteLine($"Verdi Laser received get power request, current power level is {Power}");
      }

      else if(stringData.StartsWith("?S"))
      {
        // Get Shutter Request
        Console.WriteLine($"Verdi Laser received get shutter request, current shutter status is {ToShutterStatus(Shutter)}");
      }

      else if(stringData.StartsWith("P="))
      {
        //Set Power Request
        var parsed = double.TryParse(stringData.Split("=")[1], out var power);
        if(parsed)
        {
          Console.WriteLine($"Received set power request, setting power to {power}");
          Power = power;
        }

        else
        {
          Console.WriteLine("Failed to parse power request!");
        }
      }

      else if(stringData.StartsWith("S="))
      {
        //Set Shutter Request
        Console.WriteLine("Laser received a set shutter request!");

        if(stringData.Contains("1"))
          Shutter = true;

        else
          Shutter = false;
      }

      else
        Console.WriteLine("Invalid Laser Command Received!");
    }

    private string ToShutterStatus(bool status)
    {
      if(status)
        return "Open";

      else
        return "Closed";
    }

    public double Power { get; set; } = 0.0;

    public bool Shutter { get; set; } = false;
  }
}
