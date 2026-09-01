using Ares.Core.Device.Sila;
using Ares.Core.Grpc.Services;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace UI.Features.Devices.Sila;

public partial class SilaDeviceSettingsViewModel : ReactiveObject
{
  private readonly ISilaDeviceManager _silaDeviceManager;

  public SilaDeviceSettingsViewModel(SilaDevice silaDevice, ISilaDeviceManager silaDeviceManager, Func<Task> onRemoveCallback)
  {
    _silaDeviceManager = silaDeviceManager;

    Name = silaDevice.Name;
    Id = silaDevice.UniqueId;
    Device = silaDevice;
    Address = silaDevice.Address;

    Description = Device?.Description ?? string.Empty;
    SettingsSchema = Device?.SettingSchema ?? new AresStructSchema();
    Settings = new AresStruct();

    RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
  }

  private async Task RemoveAsync(Func<Task> onRemoveCallback)
  {
    await _silaDeviceManager.RemoveDevice(Id);
    await onRemoveCallback();
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

  public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
}

