
/**
using Ares.Messaging.Device;
using ReactiveUI;
using RestSerialDevice.Config;
using RestSerialDevice.Services;
using System.Collections.Generic; // For List<string>
using System.Collections.ObjectModel; // For ObservableCollection (useful for UI updates)
using System.Reactive; // For ReactiveCommand
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public class SerialRestDeviceConfigEditViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _genericSerialRpcClient;
  private readonly RestSerialConfig _config;
  private string? _name;

  public SerialRestDeviceConfigEditViewModel(RestSerialDeviceRpc.RestSerialDeviceRpcClient genericSerialRpcClient,
    AresDevices.AresDevicesClient deviceClient,
    RestSerialConfig config)
  {
    _genericSerialRpcClient = genericSerialRpcClient;
    _devicesClient = deviceClient;
    _config = config;
  }
  
  
}
**/

using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using RestSerialDevice.Config;
using RestSerialDevice.Services;
using Microsoft.Build.Framework;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public partial class SerialRestDeviceConfigEditViewModel : ReactiveObject
{
  private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _client;
  private readonly RestSerialConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public SerialRestDeviceConfigEditViewModel(RestSerialDeviceRpc.RestSerialDeviceRpcClient client, AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new RestSerialConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public SerialRestDeviceConfigEditViewModel(RestSerialDeviceRpc.RestSerialDeviceRpcClient client, AresDevices.AresDevicesClient devicesClient, RestSerialConfig config)
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
  public partial IEnumerable<string>? AvailablePorts { get; private set; }

  public bool Modified => _config.Name != Name || _config.PortName != Port || _config.Simulated != Simulated;

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  public RestSerialConfig Save()
    => Modified ? new RestSerialConfig { Name = Name, PortName = Port, Simulated = Simulated } : _config;
}
