using System;
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

  protected AresDevice(string name, string id)
  {
    Name = name;
    UniqueId = id;
    Status = new DeviceOperationalStatus
    { OperationalState = OperationalState.Inactive, Message = $"{Name} constructed. Activation has not been called yet." };
  }

  public string Name { get; }
  public DeviceOperationalStatus Status { get; protected set; }

  public string Version { get; protected set; } = "_._._";

  public string Type { get; protected set; } = "";

  public string Description { get; protected set; } = "";

  public string UniqueId { get; init; } = Guid.NewGuid().ToString();

  public abstract Task<bool> Activate();
  public abstract Task EnterSafeMode();
}
