using Ares.Device.Serial;

namespace TicStepperController;
public class StepperControllerConnection : AresHardwareConnection, IStepperControllerConnection
{
  public StepperControllerConnection(string portName, SerialConnectionOptions? options = null)
    : base(new SerialPortConnectionInfo(
      9600,
      System.IO.Ports.Parity.None,
      8,
      System.IO.Ports.StopBits.One), portName, options)
  {
  }
}
