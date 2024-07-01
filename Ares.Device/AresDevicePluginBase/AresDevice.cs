using Ares.Messaging.Device;
using System.Threading.Tasks;

namespace Ares.Device;

public abstract class AresDevice : IAresDevice
{
  protected AresDevice(string name)
  {
    Name = name;
    Status = new DeviceStatus
    { DeviceState = DeviceState.Inactive, Message = $"{Name} constructed. Activation has not been called yet." };
  }

  public string Name { get; }
  public DeviceStatus Status { get; protected set; }

  public abstract Task<bool> Activate();
}
