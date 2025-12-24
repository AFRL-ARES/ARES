using Ares.Services.Device;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Services;
using Google.Protobuf.WellKnownTypes;
using HerkulexDRS.Config;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device.ChemyxPump;

public partial class ChemyxPumpConfigEditViewModel : ReactiveObject
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _pumpClient;
  private readonly ChemyxPumpConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public ChemyxPumpConfigEditViewModel(ChemyxPumpRpc.ChemyxPumpRpcClient client, AresDevices.AresDevicesClient devicesClient)
  {
    _pumpClient = client;
    _devicesClient = devicesClient;
    _config = new ChemyxPumpConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public ChemyxPumpConfigEditViewModel(ChemyxPumpRpc.ChemyxPumpRpcClient client, AresDevices.AresDevicesClient devicesClient, ChemyxPumpConfig config)
  {
    _pumpClient = client;
    _devicesClient = devicesClient;
    _config = config;
    _ = UpdateAvailableSerialPorts();
    _name = config.Name;
    Port = config.PortName;
    Simulated = config.Simulated;
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
  public bool NewConfig { get; set; }
  public bool Simulated { get; set; }
  public bool DualPump { get; set; }

  [Reactive]
  public partial IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _config.Name != Name || _config.PortName != Port || _config.Simulated != Simulated || _config.DualPump != DualPump;

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  public ChemyxPumpConfig Save()
    => Modified ? new ChemyxPumpConfig { Name = Name, PortName = Port, Simulated = Simulated, DualPump = DualPump } : _config;
}
