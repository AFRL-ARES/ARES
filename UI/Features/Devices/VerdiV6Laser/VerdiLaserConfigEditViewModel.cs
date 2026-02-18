using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.ComponentModel.DataAnnotations;
using VerdiV6.Config;
using VerdiV6.Services;

namespace UI.Features.Devices.VerdiV6Laser
{
  public partial class VerdiLaserConfigEditViewModel : ReactiveObject
  {
    private readonly VerdiV6Rpc.VerdiV6RpcClient _laserClient;
    private readonly VerdiConfig _config;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private string? _name;

    public VerdiLaserConfigEditViewModel(VerdiV6Rpc.VerdiV6RpcClient laserClient, AresDevices.AresDevicesClient devicesClient)
    {
      _laserClient = laserClient;
      _devicesClient = devicesClient;
      _config = new VerdiConfig();
      NewConfig = true;
      _ = UpdateAvailableSerialPorts();
    }

    public VerdiLaserConfigEditViewModel(VerdiV6Rpc.VerdiV6RpcClient laserClient, AresDevices.AresDevicesClient devicesClient, VerdiConfig config)
    {
      _laserClient = laserClient;
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
    public partial IEnumerable<string>? AvailablePorts { get; private set; }

    public bool Modified => _config.Name != Name || _config.PortName != Port || _config.Simulated != Simulated;

    public async Task UpdateAvailableSerialPorts()
    {
      AvailablePorts = null;
      Port = null;
      var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
      AvailablePorts = ports.SerialPorts;
    }

    public VerdiConfig Save()
      => Modified ? new VerdiConfig { Name = Name, PortName = Port, Simulated = Simulated } : _config;
  }
}
