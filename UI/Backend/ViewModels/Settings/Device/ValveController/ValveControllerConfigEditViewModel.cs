using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.ComponentModel.DataAnnotations;
using ValveController.Config;
using ValveController.Services;
using static Ares.Messaging.Device.AresDevices;

namespace UI.Backend.ViewModels.Settings.Device.ValveController;

public class ValveControllerConfigEditViewModel : ReactiveObject
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;
  private readonly ValveControllerConfig _config;
  private readonly AresDevicesClient _devicesClient;
  private string? _deviceName;

  public ValveControllerConfigEditViewModel(ValveControllerRpc.ValveControllerRpcClient client, AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new ValveControllerConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public ValveControllerConfigEditViewModel(ValveControllerRpc.ValveControllerRpcClient client, AresDevicesClient devicesClient, ValveControllerConfig config)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new ValveControllerConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
    _deviceName = config.Name;
    Port = config.PortName;
    Simulated = config.Simulated;
  }

  [Required]
  public string? Name
  {
    get => _deviceName;

    set
    {
      if (!NewConfig)
        return;

      _deviceName = value;
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

  public ValveControllerConfig Save()
    => Modified ? new ValveControllerConfig { Name = Name, PortName = Port, Simulated = Simulated } : _config;

}
