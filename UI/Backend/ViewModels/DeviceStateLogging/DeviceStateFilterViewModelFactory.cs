using Ares.Messaging;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateFilterViewModelFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  readonly ICombinedDeviceStateIdGetter _deviceIdGetter;

  public DeviceStateFilterViewModelFactory(AresAutomation.AresAutomationClient automationClient, ICombinedDeviceStateIdGetter deviceIdGetter)
  {
    _deviceIdGetter = deviceIdGetter;
    _automationClient = automationClient;
  }

  public DeviceStateFilterViewModel Create() => new DeviceStateFilterViewModel(_automationClient, _deviceIdGetter);
}
