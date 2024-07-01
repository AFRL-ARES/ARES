using System.ComponentModel.DataAnnotations;
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tc0304.Config;
using Tc0304.Services;

namespace UI.Backend.ViewModels.Settings.Device.Tc0304;

public class Tc0304ConfigEditViewModel : ReactiveObject
{
  private readonly TC0304Rpc.TC0304RpcClient _client;
  private readonly Tc0304Config _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public Tc0304ConfigEditViewModel(TC0304Rpc.TC0304RpcClient client, AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new Tc0304Config();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public Tc0304ConfigEditViewModel(TC0304Rpc.TC0304RpcClient client, AresDevices.AresDevicesClient devicesClient, Tc0304Config config)
  {
    _client = client;
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

  public Tc0304Config Save()
    => Modified ? new Tc0304Config { Name = Name, PortName = Port, Simulated = Simulated } : _config;
}
