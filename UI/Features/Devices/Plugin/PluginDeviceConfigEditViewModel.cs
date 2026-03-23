using Ares.Core.Device.Plugins.Drivers;
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

  public PluginDeviceConfigEditViewModel(DeviceConfig deviceConfig, DeviceDriver driver, bool isNew, DevicesService devicesService)
  {
    AvailableSerialPorts = [];
    SerialConnection = deviceConfig.SerialInfo;
    BaudRateOptions = [];
    UnitIdHint = string.Empty;
    SerialUnitId = string.Empty;
    SelectedSerialPort = string.Empty;

    NewConfig = isNew;
    Driver = driver;
    _originalConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
    _name = _originalConfig.DeviceName;
    _devicesService = devicesService;

    // A reactive property that tracks if the view model has been modified.
    _modified = this.WhenAnyValue(x => x.Name)
        .Select(_ => Name != _originalConfig.DeviceName)
        .Select(_ => _originalConfig.DeviceSettings != DeviceSettings)
        .ToProperty(this, x => x.Modified, initialValue: false);

    DriverSettingsSchema = driver.DriverSettings ?? new AresStructSchema();
    ConnectionType = driver.ConnectionType;
    DeviceSettings = isNew ? new AresStruct() : deviceConfig.DeviceSettings;
    IsSimulated = isNew ? false : deviceConfig.IsSimulated;


    if(ConnectionType == ConnectionType.Serial)
      InitializeSerialSettings();
  }

  private void InitializeSerialSettings()
  {
    var serialSettings = Driver.Manifest.SerialSettings;
    AvailableSerialPorts = _devicesService.GetServerSerialPorts(new Empty(), null).Result.SerialPorts.ToList();
    SelectedSerialPort = _originalConfig.SerialInfo?.PortName ?? string.Empty;
    if(serialSettings is not null)
    {
      if(serialSettings.VariableBaudRate)
      {
        BaudRateOptions = serialSettings.AllowedBaudRates ?? [];
        SelectedBaudRate = serialSettings.DefaultBaudRate;
      }

      if(serialSettings.RequiresUnitId && _originalConfig.SerialInfo is not null)
      {
        RequiresId = true;
        UnitIdHint = serialSettings.UnitIdValidationHint;
        IdRegex = serialSettings.UnitIdRegex ?? "[\\s\\S]";
        SerialUnitId = _originalConfig.SerialInfo.HasSerialId ? _originalConfig.SerialInfo.SerialId : string.Empty;
      }
    }  
  }

  public AresValue? GetMatchingSettingValue(string key) 
    => _originalConfig.DeviceSettings?.Fields.FirstOrDefault(f => f.Key == key).Value ?? null;

  public DeviceConfig Save()
  {
    if(Modified)
    {
      var newConfig = new DeviceConfig();
      newConfig.DeviceName = Name;
      newConfig.DriverId = Driver.UniqueId;
      newConfig.DeviceSettings = DeviceSettings;
      newConfig.IsSimulated = IsSimulated;

      if(ConnectionType == ConnectionType.Serial)
      {
        newConfig.SerialInfo = new SerialConnection();
        newConfig.SerialInfo.PortName = SelectedSerialPort;
        newConfig.SerialInfo.BaudRate = SelectedBaudRate;
        
        if(RequiresId)
          newConfig.SerialInfo.SerialId = SerialUnitId; 
      }

      return newConfig;
    }

    return _originalConfig;
  }

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
  public partial SerialConnection SerialConnection { get; set; }
  [Reactive]
  public partial bool RequiresId { get; set; }
  [Reactive]
  public partial string? IdRegex { get; set; }
  [Reactive]
  public partial List<int> BaudRateOptions { get; set; }
  [Reactive]
  public partial string UnitIdHint { get; set; }
  [Reactive]
  public partial string SerialUnitId { get; set; }
  [Reactive]
  public partial int SelectedBaudRate { get; set; }
  [Reactive]
  public partial string SelectedSerialPort { get; set; }
  [Reactive]
  public partial bool IsSimulated { get; set; }
}
