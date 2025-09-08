using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace DemoDevice;

public class DemoDeviceInterpreter : DeviceCommandInterpreter<AresDemoDevice, DemoDeviceCommand>
{
  public DemoDeviceInterpreter(AresDemoDevice device) : base(device)
  {
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(DemoDeviceCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new CommandResult();
    result.Success = true;

    switch(deviceCommandEnum)
    {
      case DemoDeviceCommand.SetTemperature:
        var value = parameters.First();
        if(!value.Value.HasNumberValue)
          throw new InvalidOperationException("The Demo Devices' SetTemperature command requires a number value as a parameter, but none was provided!");

        await Device.SetTemperature(value.Value.NumberValue);
        result.Result = AresStructHelper.CreateNumberStruct("SetTemperature", value.Value.NumberValue);
        break;
      case DemoDeviceCommand.GetTemperature:
        var temp = await Device.GetTemperature();
        result.Result = AresStructHelper.CreateNumberStruct("Temperature", temp.Value);
        break;
      case DemoDeviceCommand.GetCurrentGrowth:
        var growth = await Device.GetGrowth();
        result.Result = AresStructHelper.CreateNumberStruct("Growth", growth.Growth);
        break;
      case DemoDeviceCommand.GetCurrentPillar:
        var pillar = await Device.GetCurrentPillar();
        result.Result = AresStructHelper.CreateNumberStruct("CurrentPillar", pillar.Pillar);
        break;
      case DemoDeviceCommand.MoveToNextPillar:
        var nextPillar = await Device.MoveToNextPillar();
        result.Result = AresStructHelper.CreateNumberStruct("NewPillar", nextPillar.Pillar);
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
        DeviceId = Device.UniqueId,
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false), Name = DemoDeviceCommandParameter.Temperature.ToString(), Unit = "Degrees Celsius"} }
      },
      new()
      {
        Name = DemoDeviceCommand.GetTemperature.ToString(),
        Description = "Gets the current temperature of the demo device.",
        DeviceId = Device.UniqueId,
        OutputMetadata = new OutputMetadata { Description = "Resulting Temp", DataSchema = AresSchemaHelper.CreateSchema("Temperature", AresDataType.Number), Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.GetCurrentGrowth.ToString(),
        Description = "Gets the current growth of the demo object within the device.",
        DeviceId = Device.UniqueId,
        OutputMetadata = new OutputMetadata { Description = "Object growth", DataSchema = AresSchemaHelper.CreateSchema("Growth", AresDataType.Number), Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.GetCurrentPillar.ToString(),
        Description = "Gets the current pillar that the device is focused on.",
        DeviceId = Device.UniqueId,
        OutputMetadata = new OutputMetadata { Description = "Current Pillar", DataSchema = AresSchemaHelper.CreateSchema("Current Pillar", AresDataType.Number), Index = 0, UniqueId = Guid.NewGuid().ToString() },
      },
      new()
      {
        Name = DemoDeviceCommand.MoveToNextPillar.ToString(),
        Description = "Increases the pillar index by 1 and resets growth.",
        DeviceId = Device.UniqueId,
        OutputMetadata = new OutputMetadata { Description = "Current Pillar", DataSchema = AresSchemaHelper.CreateSchema("New Pillar", AresDataType.Number), Index = 0, UniqueId = Guid.NewGuid().ToString() },
      }
    };
  }
}
