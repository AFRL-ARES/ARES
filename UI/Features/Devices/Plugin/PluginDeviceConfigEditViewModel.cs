using Ares.Core.Device;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceConfigEditViewModel : ReactiveObject
{
  private readonly DeviceConfig _originalConfig;
  private readonly ObservableAsPropertyHelper<bool> _modified;

  private string _name;

  public PluginDeviceConfigEditViewModel(DeviceDriver driver) : this(new DeviceConfig(), driver, isNew: true)
  {
    
  }

  public PluginDeviceConfigEditViewModel(DeviceConfig deviceConfig, DeviceDriver driver) : this(deviceConfig, driver, isNew: false)
  {
    
  }

  private PluginDeviceConfigEditViewModel(DeviceConfig deviceConfig, DeviceDriver driver, bool isNew)
  {
    NewConfig = isNew;
    Driver = driver;
    _originalConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
    _name = _originalConfig.DeviceName;

    // A reactive property that tracks if the view model has been modified.
    _modified = this.WhenAnyValue(x => x.Name)
        .Select(_ => Name != _originalConfig.DeviceName)
        .ToProperty(this, x => x.Modified, initialValue: false);

    DriverSettingsSchema = driver.DriverSettings ?? new AresStructSchema();

    if(isNew)
      DeviceSettings = new AresStruct();
  }

  public AresValue? GetMatchingSettingValue(string key) 
    => _originalConfig.DriverSettings?.Fields.FirstOrDefault(f => f.Key == key).Value ?? null;

  public DeviceConfig Save()
    => Modified ? new DeviceConfig
    {
      DeviceName = Name,
      DriverId = Driver.UniqueId,
      DriverName = Driver.DriverType.ToString(),
      DriverSettings = DeviceSettings ?? new AresStruct()
    } : _originalConfig;

  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }

  public bool Modified => _modified.Value;
  public bool NewConfig { get; }
  public DeviceDriver Driver { get; }
  public AresStructSchema DriverSettingsSchema { get; }

  [Reactive]
  public partial AresStruct DeviceSettings { get; set; }
}
