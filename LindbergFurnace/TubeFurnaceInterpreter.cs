using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using LindbergFurnace.Commands;
using UnitsNet;
using Ares.Datamodel.Factories;

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
        var currentSetpoint = await Device.GetSetpoint();
        result.Result = AresStructHelper.CreateNumberStruct("Setpoint", currentSetpoint);
        break;

      case TubeFurnaceCommand.GetCurrentTemperature:
        var currentTemperature = await Device.GetCurrentTemperature();
        result.Result = AresStructHelper.CreateNumberStruct("Temperature", currentTemperature);
        break;

      case TubeFurnaceCommand.SetAndWaitForSetpoint:
        var waitedSetpoint = parameters.FirstOrDefault(param => param.Metadata.Name == TubeFurnaceParameter.Setpoint.ToString());
        var delta = parameters.FirstOrDefault(param => param.Metadata.Name == TubeFurnaceParameter.TemperatureDelta.ToString());
        var timeout = parameters.FirstOrDefault(param => param.Metadata.Name == TubeFurnaceParameter.Timeout.ToString());

        if(waitedSetpoint is null || delta is null || timeout is null)
        {
          result.Success = false;
          result.Error = "Tried to set and wait for furnace setpoint, but not all parameters were provided!";
          break;
        }

        var tempWaitedSetpoint = new Temperature(waitedSetpoint.Value.NumberValue, UnitsNet.Units.TemperatureUnit.DegreeCelsius);

        await Device.SetAndWaitForSetpoint(tempWaitedSetpoint, delta.Value.NumberValue, timeout.Value.NumberValue);
        break;

      default:
        throw new InvalidOperationException("Received an unknown command type for the Tube Furnace!");
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return
    [
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
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = "Setpoint", Unit = "Degrees Celsius", Schema = AresSchemaBuilder.Entry(AresDataType.Number).AsOptional().Build() } }
      },

      new() {
        DeviceId = Device.UniqueId,
        Name = TubeFurnaceCommand.GetCurrentTemperature.ToString(),
        Description = "Get's the current temperature of the tube furnace.",
        OutputMetadata = new OutputMetadata() 
        { 
          Index = 0,
          Description = "The current temperature of the Tube Furnace",
          DataSchema = AresSchemaBuilder.Create("Setpoint", AresDataType.Number).Build()
        }
      },

      new()
      {
        DeviceId = Device.UniqueId,
        Name = TubeFurnaceCommand.SetAndWaitForSetpoint.ToString(),
        Description = "Set's an updated set point for the tube furnace, and waits until the furnace reaches that temperature within the defined delta or the timeout value is exceeded. A negative one timeout will be treated as no timeout.",
        ParameterMetadatas = 
        { 
          new ParameterMetadata 
          { 
            Index = 0, 
            Name = TubeFurnaceParameter.Setpoint.ToString(), 
            Unit = "Degrees Celsius", 
            Schema = AresSchemaBuilder.Entry(AresDataType.Number).AsOptional().Build() 
          },
          
          new ParameterMetadata
          {
            Index = 1,
            Name = TubeFurnaceParameter.Timeout.ToString(),
            Unit = "Seconds",
            Schema = AresSchemaBuilder.Entry(AresDataType.Number).AsOptional().Build()
          },

          new ParameterMetadata
          {
            Index = 2,
            Name = TubeFurnaceParameter.TemperatureDelta.ToString(),
            Unit = "Delta",
            Schema = AresSchemaBuilder.Entry(AresDataType.Number).AsOptional().Build()
          }
        }
      }
    ];
  }
}
