using System.ComponentModel.DataAnnotations;
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TicStepperController.Config;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.StepperController;

public class StepperControllerConfigEditViewModel : ReactiveObject
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _stepperClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly StepperControllerConfig _config;
  private string _name = string.Empty;

  public StepperControllerConfigEditViewModel(StepperControllerRpc.StepperControllerRpcClient stepperClient, AresDevices.AresDevicesClient devicesClient)
  {
    _stepperClient = stepperClient;
    _devicesClient = devicesClient;
    _config = new StepperControllerConfig();
    NewConfig = true;
    _ = UpdateAvailableSerialPorts();
  }

  public StepperControllerConfigEditViewModel(StepperControllerRpc.StepperControllerRpcClient stepperClient, AresDevices.AresDevicesClient devicesClient,
    StepperControllerConfig config)
  {
    _stepperClient = stepperClient;
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

  public void LoadConfig(StepperControllerConfig config)
  {
    Port = config.PortName;
    _name = config.Name;
    MaxAcceleration = config.MaxAcceleration;
    MaxDeceleration = config.MaxDeceleration;
    StartingSpeed = config.StartingSpeed;
    CustomStepSize = config.CustomStepSize;
    MaxSpeed = config.MaxSpeed;
    StepMode = config.StepMode;
    Simulated = config.Simulated;
  }

  [Reactive]
  public IEnumerable<string>? AvailablePorts { get; private set; }

  public async Task UpdateAvailableSerialPorts()
  {
    AvailablePorts = null;
    Port = null;
    var ports = await _devicesClient.GetServerSerialPortsAsync(new Empty());
    AvailablePorts = ports.SerialPorts;
  }

  [Reactive]
  [Required]
  public string? Port { get; set; }

  [Reactive]
  [Range(100, 2_147_483_647, ErrorMessage = "Acceleration must be a value from 100 to 2,147,483,647 microsteps per 100 s²")]
  public uint? MaxAcceleration { get; set; }

  [Reactive]
  [Range(100, 2_147_483_647, ErrorMessage = "Deceleration must be a value from 100 to 2,147,483,647 microsteps per 100 s²")]
  public uint? MaxDeceleration { get; set; }

  [Reactive]
  [Range(0, 500_000_000, ErrorMessage = "Starting speed must be a value from 0 to 500,000,000 microsteps per 10,000 s")]
  public uint? StartingSpeed { get; set; }

  [Reactive]
  [Range(1, uint.MaxValue, ErrorMessage = "Custom step size must be greater than 0")]
  public uint? CustomStepSize { get; set; } = 1;

  [Reactive]
  [Range(100, 500_000_000, ErrorMessage = "Max speed must be a value from 0 to 500,000,000 microsteps per 10,000 s")]
  public uint? MaxSpeed { get; set; }

  [Reactive]
  public StepMode StepMode { get; set; }

  [Reactive]
  public bool Simulated { get; set; }

  public bool Modified => _config.Name != Name
        || _config.PortName != Port
        || _config.MaxAcceleration != MaxAcceleration
        || _config.MaxDeceleration != MaxDeceleration
        || _config.StartingSpeed != StartingSpeed
        || _config.CustomStepSize != CustomStepSize
        || _config.MaxSpeed != MaxSpeed
        || _config.StepMode != StepMode
        || _config.Simulated != Simulated;

  public StepperControllerConfig Save()
  {
    return Modified ? new StepperControllerConfig
    {
      Name = Name,
      Simulated = Simulated,
      StepMode = StepMode,
      CustomStepSize = CustomStepSize,
      MaxAcceleration = MaxAcceleration,
      MaxDeceleration = MaxDeceleration,
      StartingSpeed = StartingSpeed,
      MaxSpeed = MaxSpeed,
      PortName = Port
    } : _config;
  }
}
