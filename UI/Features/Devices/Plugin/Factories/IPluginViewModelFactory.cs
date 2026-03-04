using Ares.Device;
using Ares.Toolkit.Device.UI;

namespace UI.Features.Devices.Plugin.Factories;

public interface IPluginViewModelFactory
{
  Task<PluginDeviceSettingsListViewModel?> CreateListViewModelAsync(string driverName);
  IDeviceUnitControlViewModel CreateUnitControlViewModel(IAresDevice device);
}
