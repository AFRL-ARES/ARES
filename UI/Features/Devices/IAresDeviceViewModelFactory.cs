using Ares.Device;
using Ares.Toolkit.Device.UI;
using UI.Features.Devices.Plugin;

namespace UI.Features.Devices;

public interface IAresDeviceViewModelFactory
{
  Task<PluginDeviceSettingsListViewModel?> CreateListViewModelAsync(string driverName);
  IDeviceUnitControlViewModel CreateUnitControlViewModel(IAresDevice device);
}
