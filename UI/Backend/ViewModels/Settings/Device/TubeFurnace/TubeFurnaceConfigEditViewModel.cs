using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.ComponentModel.DataAnnotations;
using Ares.Services.Device;
using TubeFurnace.Config;
using TubeFurnace.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.TubeFurnace
{
  public partial class TubeFurnaceConfigEditViewModel : ReactiveObject
  {
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private TubeFurnaceConfig _config;
    private string _name = string.Empty;

    public TubeFurnaceConfigEditViewModel(TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient, AresDevices.AresDevicesClient devicesClient)
    {
      _tubeFurnaceClient = tubeFurnaceClient;
      _devicesClient = devicesClient;
      _config = new TubeFurnaceConfig();
      NewConfig = true;
      _ = UpdateAvailableSerialPorts();
    }

    public TubeFurnaceConfigEditViewModel(TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient, AresDevices.AresDevicesClient devicesClient,
      TubeFurnaceConfig config)
    {
      _tubeFurnaceClient = tubeFurnaceClient;
      _devicesClient = devicesClient;
      _config = config;
      _ = UpdateAvailableSerialPorts();
      LoadConfig(config);
    }

    public bool NewConfig { get; private set; }

    [Required]
    public string Name
    {
      get => _name;
      set
      {
        if (!NewConfig)
        {
          return;
        }

        this.RaiseAndSetIfChanged(ref _name, value);
      }
    }

    public void LoadConfig(TubeFurnaceConfig config)
    {
      Port = config.PortName;
      _name = config.Name;
      Simulated = config.Simulated;
      Address = config.Address;
    }

    [Reactive]
    public partial IEnumerable<string>? AvailablePorts { get; private set; }

    public async Task UpdateAvailableSerialPorts()
    {
      AvailablePorts = null;
      Port = null;
      var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
      AvailablePorts = ports.SerialPorts;
    }

    [Reactive]
    [Required]
    public partial string? Port { get; set; }

    [Required]
    public int? Address { get; set; }


    [Reactive]
    public partial bool Simulated { get; set; }

    public bool Modified => _config.Name != Name
          || _config.PortName != Port
          || _config.Simulated != Simulated
          || _config.Address != Address;

    public TubeFurnaceConfig Save()
    {
      return Modified ? new TubeFurnaceConfig
      {
        Name = Name,
        Simulated = Simulated,
        PortName = Port,
        Address = Address ?? 0,
      } : _config;
    }
  }
}
