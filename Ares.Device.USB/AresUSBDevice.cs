namespace Ares.Device.USB;
public abstract class AresUSBDevice : AresDevice, IAresUSBDevice
{
  protected AresUSBDevice(string name) : base(name)
  {
  }
}
