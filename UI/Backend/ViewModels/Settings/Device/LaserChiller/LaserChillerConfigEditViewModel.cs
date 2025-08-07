using Chiller.Config;
using Chiller.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel.DataAnnotations;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.Settings.Device.LaserChiller;

public class LaserChillerConfigEditViewModel : ReactiveObject
{
  private readonly ChillerRpc.ChillerRpcClient _chillerClient;
  private readonly ChillerConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public LaserChillerConfigEditViewModel(ChillerRpc.ChillerRpcClient chillerClient, AresDevices.AresDevicesClient devicesClient)
  {
    _chillerClient = chillerClient;
    _devicesClient = devicesClient;
    _config = new ChillerConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }
  public LaserChillerConfigEditViewModel(ChillerRpc.ChillerRpcClient chillerClient, AresDevices.AresDevicesClient devicesClient, ChillerConfig config)
  {
    _chillerClient = chillerClient;
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

  [Reactive]
  public IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _config.Name != Name || _config.PortName != Port || _config.Simulated != Simulated;

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  public ChillerConfig Save()
    => Modified ? new ChillerConfig { Name = Name, PortName = Port, Simulated = Simulated } : _config;
}
