using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Devices.Sila;

public partial class SilaDeviceSettingsViewModel : ReactiveObject
{
  public SilaDeviceSettingsViewModel(SilaDevice silaDevice)
  {
    Name = silaDevice.Name;
    Id = silaDevice.UniqueId;
    Device = silaDevice;
    Address = silaDevice.Address;

    Description = Device?.Description ?? string.Empty;
    SettingsSchema = Device?.SettingSchema ?? new AresStructSchema();
    Settings = new AresStruct();
  }

  public Task<DeviceOperationalStatus> GetOperationalStatus()
    => Task.FromResult(Device?.Status ?? new DeviceOperationalStatus { OperationalState = OperationalState.Inactive, Message = "Status Current Unknown"});

  public AresValue? GetMatchingSettingValue(string key)
  => Settings?.Fields.FirstOrDefault(f => f.Key == key).Value ?? null;

  public SilaDevice? Device { get; set; }

  [Reactive]
  public partial string Name { get; private set; }

  [Reactive]
  public partial string Id { get; private set; }

  [Reactive]
  public partial string Description { get; set; }

  [Reactive]
  public partial AresStructSchema SettingsSchema { get; private set; }

  [Reactive]
  public partial AresStruct Settings { get; set; }

  [Reactive]
  public partial string Address { get; private set; }
}

