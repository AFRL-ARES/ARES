
namespace RestSerialDevice.Simulation;

public class SimulatedRestDevice
{
  private readonly Action<byte[]> _byteSender;

  public SimulatedRestDevice(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }
}
