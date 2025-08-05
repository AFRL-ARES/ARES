namespace Ares.Device.Rest;

public abstract class AresRestDevice : AresDevice, IAresRestDevice
{
  protected AresRestDevice(string name) : base(name)
  {
  }
}
