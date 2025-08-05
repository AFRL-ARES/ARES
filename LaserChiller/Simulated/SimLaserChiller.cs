using System.Text;

namespace LaserChiller.Simulated;

public class SimLaserChiller
{
  private readonly Action<byte[]> _byteSender;
  private static readonly byte[] _setStandbyCommandData = { 0x2E, 0x47, 0x30, 0x41, 0x35, 0x0D };
  private static readonly byte[] _setRunModeCommandData = { 0x2E, 0x47, 0x31, 0x41, 0x36, 0x0D };
  private static readonly byte[] _readManifoldTempCommandData = { 0x2E, 0x49, 0x37, 0x37, 0x0D };

  public SimLaserChiller(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public void SendCommand(byte[] data)
  {
    if(data.Equals(_setStandbyCommandData))
    {
      SetChillerToStandby();
      Console.WriteLine("Laser Chiller is now in standby mode.");
    }

    else if(data.Equals(_setRunModeCommandData))
    {
      SetChillerToRunning();
      Console.WriteLine("Laser Chiller is now running!");
    }

    else if(data.SequenceEqual(_readManifoldTempCommandData))
    {
      _byteSender(BitConverter.GetBytes(ManifoldTemperature));
    }
  }

  private void SetChillerToStandby()
  {
    Mode = SimChillerModeEnum.Standby;
  }

  private void SetChillerToRunning()
  {
    Mode = SimChillerModeEnum.Running;
  }

  public SimChillerModeEnum Mode { get; set; }

  public double ManifoldTemperature { get; set; }
}
