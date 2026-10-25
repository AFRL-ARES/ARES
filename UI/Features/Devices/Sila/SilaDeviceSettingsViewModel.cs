using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;

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
    Address = silaDevice?.Address?.ToString() ?? "";

    Description = Device?.Description ?? string.Empty;
    SettingsSchema = Device?.SettingSchema ?? new AresStructSchema();
    Settings = new AresStruct();
    DeviceCommands = [];

    RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
  }

  private async Task RemoveAsync(Func<Task> onRemoveCallback)
  {
    await _silaDeviceManager.RemoveDevice(Id);
    await onRemoveCallback();
  }

  public async Task<DeviceOperationalStatus> GetOperationalStatus()
  {
    if(Device is null)
    {
      IsLoading = false;
      return new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Inactive,
        Message = "Status is Currently Unknown"
      };
    }

    IsLoading = true;
    try
    {
      var status = await Device.StatusObservable.FirstAsync();
      return status;
    }
    finally
    {
      IsLoading = false; // Always ensure loading state resets
    }
  }

  public async Task LoadDeviceCommandsAsync()
  {
    if(Device is null)
    {
      DeviceCommands = [];
      return;
    }

    var descriptors = await Device.GetCommandDescriptorsAsync();
    DeviceCommands = descriptors.ToArray();
  }

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

  [Reactive]
  public partial DeviceCommandDescriptor[] DeviceCommands { get; private set; }

  [Reactive]
  public partial bool IsLoading { get; private set; } = true;

  public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
}