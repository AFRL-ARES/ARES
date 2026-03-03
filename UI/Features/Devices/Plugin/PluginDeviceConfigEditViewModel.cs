using Ares.Core.Device.Drivers;
using Ares.Core.Grpc.Services;
using Ares.Core.Resources;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceConfigEditViewModel : ReactiveObject
{
  private readonly DeviceConfig _originalConfig;
  private readonly ObservableAsPropertyHelper<bool> _modified;
  private readonly DevicesService _devicesService;
  private string _name;

  public PluginDeviceConfigEditViewModel(DeviceDriver driver, DevicesService devicesService) : this(new DeviceConfig(), driver, isNew: true, devicesService) { }

  public PluginDeviceConfigEditViewModel(DeviceConfig deviceConfig, DeviceDriver driver, DevicesService devicesService) : this(deviceConfig, driver, isNew: false, devicesService) { }

  private PluginDeviceConfigEditViewModel(DeviceConfig deviceConfig, DeviceDriver driver, bool isNew, DevicesService devicesService)
  {
    NewConfig = isNew;
    Driver = driver;
    _originalConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
    _name = _originalConfig.DeviceName;
    _devicesService = devicesService;

    // A reactive property that tracks if the view model has been modified.
    _modified = this.WhenAnyValue(x => x.Name)
        .Select(_ => Name != _originalConfig.DeviceName)
        .ToProperty(this, x => x.Modified, initialValue: false);

    DriverSettingsSchema = driver.DriverSettings ?? new AresStructSchema();
    ConnectionType = driver.ConnectionType;
    AvailableSerialPorts = _devicesService.GetServerSerialPorts(new Empty(), null).Result.SerialPorts.ToList();

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
      DriverSettings = DeviceSettings ?? new AresStruct(),
      Serial = new SerialConnection { PortName = SelectedSerialPort }
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
  public ConnectionType ConnectionType { get; }
  [Reactive]
  public partial AresStruct DeviceSettings { get; set; }
  [Reactive]
  public partial List<string> AvailableSerialPorts { get; set; }
  [Reactive]
  public partial string SelectedSerialPort { get; set; }
}
