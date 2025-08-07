using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel.DataAnnotations;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public class MfcConfigEditViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly MfcConfig _mfcConfig;
  private string? _name;

  public MfcConfigEditViewModel(MfcRpc.MfcRpcClient mfcClient,
    AresDevices.AresDevicesClient devicesClient,
    MfcConfig mfcConfig
    )
  {
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    _mfcConfig = mfcConfig;
    _ = UpdateAvailableSerialPorts();
    _name = _mfcConfig.Name;
    Id = _mfcConfig.Id;
    Port = _mfcConfig.PortName;
    Simulated = _mfcConfig.Simulated;
    HasValve = _mfcConfig.HasValve;
  }

  public MfcConfigEditViewModel(MfcRpc.MfcRpcClient mfcClient, AresDevices.AresDevicesClient devicesClient)
  {
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    _mfcConfig = new MfcConfig();
    _ = UpdateAvailableSerialPorts();
    NewConfig = true;
  }

  [Required]
  public string? Name
  {
    get => _name;

    set
    {
      if (!NewConfig)
        return;

      _name = value;
    }
  }

  [Required]
  public string? Port { get; set; }

  // TODO this is here to prevent changing name as the name of the device is used for lookup and stuff
  // but maybe we should use some kind of GUID instead to make this a bit more robust and allow name changes.
  public bool NewConfig { get; }

  [Required]
  [CharacterRange('A', 'Z', ErrorMessage = "Id must be any capital letter from A to Z")]
  [StringLength(1, ErrorMessage = "Id must be one character")]
  public string? Id { get; set; }

  public bool HasValve { get; set; } = true;

  public bool Simulated { get; set; }

  [Reactive]
  public IEnumerable<char>? AvailableIds { get; private set; }

  [Reactive]
  public IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _mfcConfig.Id != Id || _mfcConfig.Name != Name || _mfcConfig.PortName != Port || _mfcConfig.Simulated != Simulated || _mfcConfig.HasValve != HasValve;

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  public async Task UpdateAvailableIds()
  {
    AvailableIds = null;
    Id = null;
    if (Port is null)
      return;

    var ids = await _mfcClient.GetAvailableIdsAsync(new GetAvailableIdsRequest { PortName = Port, Simulated = Simulated });
    AvailableIds = ids.Ids.Select(s => s.First());
  }

  public MfcConfig Save()
    => Modified ? new MfcConfig { Id = Id, Name = Name, PortName = Port, Simulated = Simulated, HasValve = HasValve } : _mfcConfig;
}
