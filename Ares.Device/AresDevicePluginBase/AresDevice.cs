using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device;

public abstract class AresDevice : IAresDevice
{
  protected AresDevice(string name)
  {
    Name = name;
    Status = new DeviceOperationalStatus
    { OperationalState = OperationalState.Inactive, Message = $"{Name} constructed. Activation has not been called yet." };
  }

  public string Name { get; }
  public DeviceOperationalStatus Status { get; protected set; }
  public abstract Task<bool> Activate();
  public abstract Task EnterSafeMode();
}
