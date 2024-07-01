using Ares.Device;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace DemoDevice;

public class DemoDeviceInterpreter : DeviceCommandInterpreter<AresDemoDevice, DemoDeviceCommand>
{
  public DemoDeviceInterpreter(AresDemoDevice device) : base(device)
  {
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(DemoDeviceCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch (deviceCommandEnum)
    {
      case DemoDeviceCommand.SetTemperature:
        var value = parameters.First().Value.Value;
        await Device.SetTemperature(value);
        break;
      case DemoDeviceCommand.GetTemperature:
        var temp = await Device.GetTemperature();
        result.Result = Any.Pack(temp);
        break;
      case DemoDeviceCommand.GetCurrentGrowth:
        var growth = await Device.GetGrowth();
        result.Result = Any.Pack(growth);
        break;
      case DemoDeviceCommand.GetCurrentPillar:
        var pillar = await Device.GetCurrentPillar();
        result.Result = Any.Pack(pillar);
        break;
      case DemoDeviceCommand.MoveToNextPillar:
        var nextPillar = await Device.MoveToNextPillar();
        result.Result = Any.Pack(nextPillar);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        Name = DemoDeviceCommand.SetTemperature.ToString(),
        Description = "Sets the temperature for the demo device",
        DeviceName = Device.Name,
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = DemoDeviceCommandParameter.Temperature.ToString() } }
      },
      new()
      {
        Name = DemoDeviceCommand.GetTemperature.ToString(),
        Description = "Gets the current temperature of the demo device.",
        DeviceName = Device.Name,
        OutputMetadata = new OutputMetadata { Description = "Resulting Temp", FullName = typeof(Temperature).FullName, Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.GetCurrentGrowth.ToString(),
        Description = "Gets the current growth of the demo object within the device.",
        DeviceName = Device.Name,
        OutputMetadata = new OutputMetadata { Description = "Object growth", FullName = typeof(GrowthResponse).FullName, Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.GetCurrentPillar.ToString(),
        Description = "Gets the current pillar that the device is focused on.",
        DeviceName = Device.Name,
        OutputMetadata = new OutputMetadata { Description = "Current Pillar", FullName = typeof(CurrentPillarResponse).FullName, Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.MoveToNextPillar.ToString(),
        Description = "Increases the pillar index by 1 and resets growth.",
        DeviceName = Device.Name,
        OutputMetadata = new OutputMetadata { Description = "Current Pillar", FullName = typeof(CurrentPillarResponse).FullName, Index = 0, UniqueId = Guid.NewGuid().ToString() },
      }
    };
  }
}
