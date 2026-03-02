using Ares.Core.Device.Providers;
using Ares.Device;
using Ares.Toolkit.Device.UI;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.Plugin;

public class PluginDeviceViewModelFactory : IDeviceViewModelFactory
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IAresDriverProvider _driverProvider;

  public PluginDeviceViewModelFactory(IServiceProvider serviceProvider, IAresDriverProvider driverProvider)
  {
    _serviceProvider = serviceProvider;
    _driverProvider = driverProvider;
  }

  public IDeviceUnitControlViewModel Create(IAresDevice device)
  {
    //TODO: This is definitely not right, figure out how to maintain an association between a device and it's driver?
    var driver = _driverProvider.GetDriverById(device.UniqueId);

    if(driver != null && driver.ViewModelType != null)
    {
      return (IDeviceUnitControlViewModel)ActivatorUtilities.CreateInstance(
          _serviceProvider,
          driver.ViewModelType,
          device);
    }

    //TODO: Implement default logic here to prevent crashes from bad device UI logic
    throw new InvalidOperationException();
    //return new DefaultDeviceViewModel(device);
  }
}
