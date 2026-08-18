using Ares.Device;
using Ares.Toolkit.Device.UI;

namespace UI.Features.Devices.Shared;

/// <summary>
/// Factory responsible for dynamically instantiating the ViewModel associated with a device's driver.
/// </summary>
public interface IDeviceViewModelFactory
{
  IDeviceUnitControlViewModel Create(IAresDevice device);
}
