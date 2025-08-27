using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using LindbergFurnace.Commands;
using UnitsNet;

namespace LindbergFurnace;

public class TubeFurnaceInterpreter : DeviceCommandInterpreter<ITubeFurnace, TubeFurnaceCommand>
{
  public TubeFurnaceInterpreter(ITubeFurnace device) : base(device)
  {
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(TubeFurnaceCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new CommandResult();
    result.Success = true;

    switch(deviceCommandEnum)
    {
      case TubeFurnaceCommand.SetSetpoint:
        var setpoint = parameters[0];
        if(!setpoint.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The furnace command SetSetpoint requires a number as a parameter, but none was provided!";
          break;
        }  

        var tempSetPoint = new Temperature(setpoint.Value.NumberValue, UnitsNet.Units.TemperatureUnit.DegreeCelsius);
        await Device.SetSetpoint(tempSetPoint);
        break;

      case TubeFurnaceCommand.GetSetpoint:
        await Device.GetSetpoint();
        break;

      case TubeFurnaceCommand.GetCurrentTemperature:
        await Device.GetCurrentTemperature();
        break;

      default:
        throw new InvalidOperationException("Received an unknown command type for the Tube Furnace!");
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceId = Device.UniqueId,
        Name = TubeFurnaceCommand.GetSetpoint.ToString(),
        Description = "Get's the updated set point for the tube furnace."
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = TubeFurnaceCommand.SetSetpoint.ToString(),
        Description = "Set's an updated set point for the tube furnace, defined by the user.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = "Setpoint", Unit = "Degrees Celsius", Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } }
      },

      new() {
        DeviceId = Device.UniqueId,
        Name = TubeFurnaceCommand.GetCurrentTemperature.ToString(),
        Description = "Get's the current temperature of the tube furnace."
      }
    };
  }
}
