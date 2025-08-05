using Ares.Messaging.Device;
using ReactiveUI;
using RestDevice.Config;
using RestDevice.Services;
using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device.RestDevice;

public class RestDeviceConfigEditViewModel : ReactiveObject
{
  private readonly RestDeviceRpc.RestDeviceRpcClient _client;
  private readonly RestDeviceConfig _config;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private string? _name;

  public RestDeviceConfigEditViewModel(RestDeviceRpc.RestDeviceRpcClient client, AresDevices.AresDevicesClient devicesClient)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = new RestDeviceConfig();
    NewConfig = true;
  }

  public RestDeviceConfigEditViewModel(RestDeviceRpc.RestDeviceRpcClient client, AresDevices.AresDevicesClient devicesClient, RestDeviceConfig config)
  {
    _client = client;
    _devicesClient = devicesClient;
    _config = config;
    _name = config.Name;
    Simulated = config.Simulated;
    Address = config.Address;
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

  public bool NewConfig { get; set; }

  public bool Simulated { get; set; }

  public string? Address { get; set; }

  public bool Modified => _config.Name != Name || _config.Address != Address;

  public RestDeviceConfig Save() => Modified ? new RestDeviceConfig { Name = Name, Address = Address, Simulated = Simulated } : _config;
}
