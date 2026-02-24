using Ares.Services;
using Ares.Services.Device;

namespace UI.Features.DeviceStateLogging;

public class DeviceStateFilterViewModelFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public DeviceStateFilterViewModelFactory(AresAutomation.AresAutomationClient automationClient, AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
    _automationClient = automationClient;
  }

  public DeviceStateFilterViewModel Create() => new DeviceStateFilterViewModel(_devicesClient, _automationClient);
}
