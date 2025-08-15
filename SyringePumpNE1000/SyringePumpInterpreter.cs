using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using UnitsNet;


namespace SyringePumpNE1000;

public class SyringePumpInterpreter : DeviceCommandInterpreter<ISyringePump, SyringePumpCommand>
{
  public SyringePumpInterpreter(ISyringePump device) : base(device)
  {
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(SyringePumpCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new CommandResult();
    result.Success = true;

    switch(deviceCommandEnum)
    {
      case SyringePumpCommand.QueryPhaseFunction:
        await Device.QueryPhaseFunction();
        break;

      case SyringePumpCommand.SetPhase:
        var phaseParam = parameters[0].Value;
        if(!phaseParam.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Syringe Pump command SetPhase requires a number as a parameter, but none was provided!";
          break;
        }

        await Device.SetPhase((int)phaseParam.NumberValue);
        break;

      case SyringePumpCommand.SetPhaseFunction:
        //TODO: Fix once parameters are fixed?
        throw new NotImplementedException();

      case SyringePumpCommand.QueryPhase:
        await Device.QueryPhase();
        break;

      case SyringePumpCommand.SetDiameter:
        var desiredDiameterParam = parameters[0].Value;

        if(!desiredDiameterParam.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Syringe Pump command SetDiameter requires a number as a parameter, but none was provided!";
          break;
        }

        var lengthItem = Length.FromMillimeters(desiredDiameterParam.NumberValue);
        await Device.SetDiameter(lengthItem);
        break;

      case SyringePumpCommand.GetDiameter:
        await Device.GetDiameter();
        break;

      case SyringePumpCommand.SetProgramFunctionRate:
        var functionRateParam = parameters[0].Value;
        if(!functionRateParam.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Syringe Pump command SetProgramFunctionRate requires a number as a parameter, but none was provided!";
          break;
        }
        var speed = Speed.FromMillimetersPerMinutes(functionRateParam.NumberValue);
        await Device.SetProgramFunctionRate(speed);
        return new CommandResult() { Success = true };

      case SyringePumpCommand.GetProgramFunctionRate:
        await Device.GetProgramFunctionRate();
        return new CommandResult { Success = true };

      case SyringePumpCommand.SetProgramFunctionVolumeToBeDispensed:
        throw new NotImplementedException();

      case SyringePumpCommand.GetProgramFunctionVolumeToBeDispensed:
        throw new NotImplementedException();

      case SyringePumpCommand.SetProgramFunctionPumpingDirection:
        throw new NotImplementedException();

      case SyringePumpCommand.GetProgramFunctionPumpingDirection:
        throw new NotImplementedException();

      case SyringePumpCommand.StartPumpingProgram:
        await Device.StartPumpingProgram();
        return new CommandResult { Success = true };

      case SyringePumpCommand.PurgePump:
        await Device.PurgePump();
        return new CommandResult { Success = true };

      case SyringePumpCommand.StopPumpingProgram:
        await Device.StopPumpingProgram();
        return new CommandResult { Success = true };

      case SyringePumpCommand.GetVolumeDispensed:
        await Device.GetVolumeDispensed();
        return new CommandResult { Success = true };

      case SyringePumpCommand.ClearVolumeDispensed:
        //await Device.ClearVolumeDispensed();
        return new CommandResult { Success = true };

      default:
        throw new NotImplementedException();
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.QueryPhaseFunction.ToString(),
        Description = string.Empty },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetPhase.ToString(),
        Description = string.Empty,
        ParameterMetadatas =
        {
          new ParameterMetadata[]
          {
            new()
            { Index = 0,
              Name = "Phase",
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
            }
          }
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetPhaseFunction.ToString(),
        Description = string.Empty,
        ParameterMetadatas =
        {
          new ParameterMetadata[]
          {
            new()
            {
              Index = 0,
              Name = "Phase Function",
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
            }
          }
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.QueryPhase.ToString(),
        Description = string.Empty,
        OutputMetadata = new OutputMetadata
        {
          Description = "The current Phase of the syringe pump.",
          DataSchema = AresSchemaHelper.CreateSchema("Phase Number", AresDataType.Number),
          UniqueId = Guid.NewGuid().ToString()
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetDiameter.ToString(),
        Description = string.Empty,
        ParameterMetadatas =
        {
          new ParameterMetadata[]
          {
            new()
            {
              Index = 0,
              Name = "Diameter",
              Unit = "Millimeters",
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
            }
          }
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.GetDiameter.ToString(),
        Description = "Gets the set diameter value as reported by the syringe pump."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetProgramFunctionRate.ToString(),
        Description = string.Empty,
        ParameterMetadatas =
        {
          new ParameterMetadata[]
          {
            new()
            {
              Index = 0,
              Name = "Function Rate",
              Unit = "mL/min",
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
            }
          }
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.GetProgramFunctionRate.ToString(),
        Description = string.Empty },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetProgramFunctionVolumeToBeDispensed.ToString(),
        Description = string.Empty,
        ParameterMetadatas = {new ParameterMetadata[] { new() { Index = 0, Name = "Volume", Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } } }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.GetProgramFunctionVolumeToBeDispensed.ToString(),
        Description = string.Empty },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.GetProgramFunctionPumpingDirection.ToString(),
        Description = string.Empty },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.SetProgramFunctionPumpingDirection.ToString(),
        Description = string.Empty,
        ParameterMetadatas = {new ParameterMetadata[] { new() { Index = 0, Name = "Pumping Direction", Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } } }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.StartPumpingProgram.ToString(),
        Description = "Starts the pumping program operation. If the pumping program was paused, then pumping program resumes at the point where it was stopped " +
        "Otherwise, the pumping program starts from phase one."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.PurgePump.ToString(),
        Description = "Starts purge. Pump infuses or withdraws at the top speed, depending on the pumping direction."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.StopPumpingProgram.ToString(),
        Description = "If the pumping program is operating, the pump will be stopped and the pumping program will be paused. If the pumping program is paused, " +
        "the stop command will cancel the pause and reset the pumping program to Phase 1."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.GetVolumeDispensed.ToString(),
        Description = string.Empty },

      new()
      {
        DeviceName = Device.Name,
        Name = SyringePumpCommand.ClearVolumeDispensed.ToString(),
        Description = "Sets the infused or withdrawn volume disepensed to 0. Command is ONLY VALID while the pumping program is not operating."
      }


    };
  }
}
