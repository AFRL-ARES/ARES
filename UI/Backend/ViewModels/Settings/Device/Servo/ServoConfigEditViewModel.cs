using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using HerkulexDRS.Config;
using HerkulexDRS.Services;
using Microsoft.Build.Framework;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.Servo;

public class ServoConfigEditViewModel : ReactiveObject
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;
  private readonly ServoConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public ServoConfigEditViewModel(HerkulexDRSRpc.HerkulexDRSRpcClient client, AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new ServoConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public ServoConfigEditViewModel(HerkulexDRSRpc.HerkulexDRSRpcClient client, AresDevices.AresDevicesClient devicesClient, ServoConfig config)
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

  public ServoConfig Save()
    => Modified ? new ServoConfig { Name = Name, PortName = Port, Simulated = Simulated } : _config;
}
