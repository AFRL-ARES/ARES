using Ares.Core.Device;
using Ares.Core.Device.Providers;
using Ares.Device;
using Ares.Toolkit.Device.UI;

namespace UI.Features.Devices.Plugin.Factories;

public class PluginViewModelFactory : IPluginViewModelFactory
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IAresDriverProvider _driverProvider;
  private readonly IDeviceConfigManager _deviceConfigManager;
  private readonly ILogger<PluginViewModelFactory> _logger;

  public PluginViewModelFactory(IServiceProvider serviceProvider, 
    IAresDriverProvider driverProvider,
    IDeviceConfigManager deviceConfigManager,
    ILogger<PluginViewModelFactory> logger)
  {
    _serviceProvider = serviceProvider;
    _driverProvider = driverProvider;
    _deviceConfigManager = deviceConfigManager;
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
        _logger.LogDebug($"AVAILABLE DRIVER + UNIQUE ID: {driver.Manifest.DeviceTypeName} - {driver.UniqueId}");
      }

      return null;
    }
  }

  //public Task<PluginDeviceSettingsViewModel> CreateSettingsViewModel(IAresDevice device)
  //{
  //  var config = _deviceConfigManager.
  //}

  public async Task<IDeviceUnitControlViewModel> CreateUnitControlViewModel(IAresDevice device)
  {
    var config = await _deviceConfigManager.GetConfig(device.UniqueId);

    if(config is null)
      return CreateDefaultViewModel(device);

    var driver = _driverProvider.GetDriverById(config.DriverId);
    
    if(driver is null || driver.ViewModelType is null)
      return CreateDefaultViewModel(device);

    try
    {
      IDeviceUnitControlViewModel viewModel = (IDeviceUnitControlViewModel)ActivatorUtilities.CreateInstance(_serviceProvider, driver.ViewModelType, [device]);
      return viewModel;
    }

    catch(Exception ex)
    {
      _logger.LogError($"Encountered an exception when trying to instantiate a device unit control view model for {device.Name}. " +
        $"Check the following error message for more details. ARES will default to a generic view model for the device dashboard.");

      _logger.LogError($"~~~~~~~~~ERROR MESSAGE~~~~~~~~~ {ex.Message}");

      return CreateDefaultViewModel(device);
    }
  }

  private IDeviceUnitControlViewModel CreateDefaultViewModel(IAresDevice device) => new PluginDeviceUnitViewModel(device);
}
