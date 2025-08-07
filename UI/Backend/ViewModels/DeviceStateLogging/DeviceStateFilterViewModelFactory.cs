using Ares.Services;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateFilterViewModelFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  readonly ICombinedDeviceIdGetter _deviceIdGetter;

  public DeviceStateFilterViewModelFactory(AresAutomation.AresAutomationClient automationClient, ICombinedDeviceIdGetter deviceIdGetter)
  {
    _deviceIdGetter = deviceIdGetter;
    _automationClient = automationClient;
  }

  public DeviceStateFilterViewModel Create() => new DeviceStateFilterViewModel(_automationClient, _deviceIdGetter);
}
