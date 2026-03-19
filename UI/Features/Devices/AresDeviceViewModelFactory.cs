using Ares.Core.Device.Providers;
using Ares.Device;
using Ares.Toolkit.Device.UI;
using UI.Application.Devices.Repos;
using UI.Features.Devices.Plugin;

namespace UI.Features.Devices;

public class AresDeviceViewModelFactory : IAresDeviceViewModelFactory
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IDeviceDriverProvider _driverProvider;
  private readonly IDeviceConfigProvider _deviceConfigProvider;
  private readonly IDeviceAdapterRepository _deviceAdapterRepo;
  private readonly ILogger<IAresDeviceViewModelFactory> _logger;

  public AresDeviceViewModelFactory(IServiceProvider serviceProvider,
    IDeviceDriverProvider driverProvider,
    IDeviceConfigProvider deviceConfigProvider,
    IDeviceAdapterRepository deviceAdapterRepo,
    ILogger<IAresDeviceViewModelFactory> logger)
  {
    _serviceProvider = serviceProvider;
    _driverProvider = driverProvider;
    _deviceConfigProvider = deviceConfigProvider;
    _deviceAdapterRepo = deviceAdapterRepo;
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

  public IDeviceUnitControlViewModel CreateUnitControlViewModel(IAresDevice device)
  {
    var config = _deviceConfigProvider.GetConfigByDeviceId(device.UniqueId);

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
