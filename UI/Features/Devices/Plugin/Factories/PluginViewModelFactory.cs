using Ares.Core.Device.Providers;

namespace UI.Features.Devices.Plugin.Factories;

public class PluginViewModelFactory : IPluginViewModelFactory
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IAresDriverProvider _driverProvider;
  private readonly ILogger<PluginViewModelFactory> _logger;

  public PluginViewModelFactory(IServiceProvider serviceProvider, 
    IAresDriverProvider driverProvider, 
    ILogger<PluginViewModelFactory> logger)
  {
    _serviceProvider = serviceProvider;
    _driverProvider = driverProvider;
    _logger = logger;
  }

  public async Task<PluginDeviceSettingsListViewModel?> CreateListViewModelAsync(string driverId)
  {
    var matchingDriver = _driverProvider.GetDriverById(driverId);

    if(matchingDriver is not null)
    {
      var vm = _serviceProvider.GetRequiredService<PluginDeviceSettingsListViewModel>();
      await vm.Initialize(matchingDriver);
      return vm;
    }

    else
    {
      _logger.LogError($"Failed to find a matching driver! A driver SHA/ID of {driverId} was provided, " +
        $"but we were unable to locate any matching drivers from our provider when trying to create our the settings list view model.");

      _logger.LogDebug("LISTING AVAILABLE DEVICE DRIVERS");
      foreach(var driver in _driverProvider.GetAllDeviceDrivers())
      {
        _logger.LogDebug($"AVILABLE DRIVER + UNIQUE ID: {driver.Manifest.DeviceTypeName} - {driver.UniqueId}");
      }

      return null;
    }

  }
}
