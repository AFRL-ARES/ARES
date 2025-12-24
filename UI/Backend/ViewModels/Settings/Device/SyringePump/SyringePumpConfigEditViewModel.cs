using System.ComponentModel.DataAnnotations;
using Ares.Services.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Settings.Device.SyringePump;

public partial class SyringePumpConfigEditViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;
  private readonly SyringePumpConfig _syringePumpConfig;
  private string? _name;

  public SyringePumpConfigEditViewModel(SyringePumpRpc.SyringePumpRpcClient syringePumpClient,
    AresDevices.AresDevicesClient devicesClient,
    SyringePumpConfig syringePumpConfig
    )
  {
    _syringePumpClient = syringePumpClient;
    _devicesClient = devicesClient;
    _syringePumpConfig = syringePumpConfig;
    _ = UpdateAvailableSerialPorts();
    _name = _syringePumpConfig.Name;
    Address = _syringePumpConfig.Address;
    Port = _syringePumpConfig.PortName;
    Simulated = _syringePumpConfig.Simulated;
  }

  public SyringePumpConfigEditViewModel(SyringePumpRpc.SyringePumpRpcClient syringePumpClient, AresDevices.AresDevicesClient devicesClient)
  {
    _syringePumpClient = syringePumpClient;
    _devicesClient = devicesClient;
    _syringePumpConfig = new SyringePumpConfig();
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
  public uint? Address { get; set; }

  public bool Simulated { get; set; }

  [Reactive]
  public partial IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _syringePumpConfig.Name != Name || _syringePumpConfig.PortName != Port || _syringePumpConfig.Simulated != Simulated || _syringePumpConfig.Address != Address;

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  public SyringePumpConfig Save()
    => Modified ? new SyringePumpConfig { Address = Address ?? 0, Name = Name, PortName = Port, Simulated = Simulated } : _syringePumpConfig;
}
