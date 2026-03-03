namespace UI.Features.Devices.Plugin.Factories;

public interface IPluginViewModelFactory
{
  Task<PluginDeviceSettingsListViewModel?> CreateListViewModelAsync(string driverName);
}
