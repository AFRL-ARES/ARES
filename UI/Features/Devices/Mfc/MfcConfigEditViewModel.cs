using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.ComponentModel.DataAnnotations;
using Enum = System.Enum;

namespace UI.Features.Devices.Mfc;

public partial class MfcConfigEditViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly MfcConfig _mfcConfig;
  private readonly ILogger<MfcConfigEditViewModel> _logger;
  private string? _name;

  public MfcConfigEditViewModel(MfcRpc.MfcRpcClient mfcClient,
    AresDevices.AresDevicesClient devicesClient,
    MfcConfig mfcConfig,
    ILogger<MfcConfigEditViewModel> logger
    )
  {
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    _mfcConfig = mfcConfig;
    _logger = logger;
    _ = UpdateAvailableSerialPorts();
    _name = _mfcConfig.Name;
    Id = _mfcConfig.Id;
    Port = _mfcConfig.PortName;
    Simulated = _mfcConfig.Simulated;
    HasValve = _mfcConfig.HasValve;
    SelectedMfcType = _mfcConfig.MfcType == MfcType.None ? MfcType.Normal : _mfcConfig.MfcType;
    SetpointSource = SetpointSource.UnknownSource;
  }

  public MfcConfigEditViewModel(MfcRpc.MfcRpcClient mfcClient, AresDevices.AresDevicesClient devicesClient, ILogger<MfcConfigEditViewModel> logger)
  {
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    _logger = logger;
    _mfcConfig = new MfcConfig();
    _ = UpdateAvailableSerialPorts();
    NewConfig = true;
    SetpointSource = SetpointSource.UnknownSource;
  }

  [Required]
  public string? Name
  {
    get => _name;

    set
    {
      if(!NewConfig)
        return;

      _name = value;
    }
  }

  [Required]
  public string? Port { get; set; }
  
  public bool NewConfig { get; }

  [Required]
  [CharacterRange('A', 'Z', ErrorMessage = "Id must be any capital letter from A to Z")]
  [StringLength(1, ErrorMessage = "Id must be one character")]
  public string? Id { get; set; }

  public bool HasValve { get; set; } = true;

  public MfcType[] AvailableMfcTypes { get; } = System.Enum.GetValues<MfcType>();

  public MfcType SelectedMfcType { get; set; } = MfcType.Normal;

  [Reactive]
  public partial SetpointSource SetpointSource { get; private set; }

  public SetpointSource[] AvailableSetpointSources { get; } =
    Enum.GetValues<SetpointSource>().Except([SetpointSource.UnknownSource]).ToArray();

  public bool Simulated { get; set; }

  [Reactive]
  public partial IEnumerable<char>? AvailableIds { get; private set; }

  [Reactive]
  public partial IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _mfcConfig.Id != Id || _mfcConfig.Name != Name || _mfcConfig.PortName != Port || _mfcConfig.Simulated != Simulated || _mfcConfig.HasValve != HasValve || _mfcConfig.MfcType != SelectedMfcType;

  public async Task UpdateSetpointSource(SetpointSource source)
  {
    if (SetpointSource == source)
      return;

    try
    {
      await _mfcClient.SetSetpointSourceAsync(new SetSetpointSourceRequest { Id = _mfcConfig.Id, Source = source });
      var newSource = await _mfcClient.GetSetpointSourceAsync(new DeviceRequest { DeviceId = _mfcConfig.Id });
      SetpointSource = newSource.Source;
    }
    catch (Exception e)
    {
      SetpointSource = SetpointSource.UnknownSource;
      _logger.LogError(e, "Failed to update setpoint source for alicat {}", _mfcConfig.Name);
    }
  }
  
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
    if(Port is null)
      return;

    var ids = await _mfcClient.GetAvailableIdsAsync(new GetAvailableIdsRequest { PortName = Port, Simulated = Simulated });
    AvailableIds = ids.Ids.Select(s => s.First());
  }

  public MfcConfig Save()
    => Modified ? new MfcConfig { Id = Id, Name = Name, PortName = Port, Simulated = Simulated, HasValve = HasValve, MfcType = SelectedMfcType } : _mfcConfig;
}
