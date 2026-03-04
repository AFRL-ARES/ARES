using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  public PluginDeviceSettingsViewModel(DeviceConfig deviceConfig)
  {
    _deviceConfig = deviceConfig;

    Name = _deviceConfig.DeviceName;
    Id = _deviceConfig.UniqueId;
    DriverName = _deviceConfig.DriverName;
  }

  [Reactive]
  public partial string Name { get; private set; }

  [Reactive]
  public partial string Id { get; private set; }

  [Reactive]
  public partial string DriverName { get; private set; }
}
