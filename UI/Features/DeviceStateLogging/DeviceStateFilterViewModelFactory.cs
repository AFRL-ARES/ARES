using Ares.Services;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;

namespace UI.Features.DeviceStateLogging;

public class DeviceStateFilterViewModelFactory
{
  private readonly AutomationService _automationClient;
  private readonly DevicesService _devicesClient;

  public DeviceStateFilterViewModelFactory(AutomationService automationClient, DevicesService devicesClient)
  {
    _devicesClient = devicesClient;
    _automationClient = automationClient;
  }

  public DeviceStateFilterViewModel Create() => new DeviceStateFilterViewModel(_devicesClient, _automationClient);
}
