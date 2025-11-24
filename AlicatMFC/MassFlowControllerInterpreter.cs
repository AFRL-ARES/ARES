using System.Reactive.Linq;
using AlicatMFC.Commands;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Device;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFC;

public class MassFlowControllerInterpreter : DeviceCommandInterpreter<IMassFlowController, MassFlowControllerCommand>
{
  public MassFlowControllerInterpreter(IMassFlowController device) : base(device)
  {
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(MassFlowControllerCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new CommandResult();

    switch(deviceCommandEnum)
    {
      case MassFlowControllerCommand.PollLiveDataFrame:
        // TODO: this is just an example and does not actually stringify the live info object
        // result.Result = ByteString.CopyFromUtf8((await Device.GetLiveDataInfoAsync(cancellationToken)).ToString());
        break;

      case MassFlowControllerCommand.ManufacturerInfo:
        // await Device.GetManufacturerDataInfoAsync(cancellationToken);
        break;

      case MassFlowControllerCommand.CancelValveHold:
        await Device.CancelValveHold();
        break;

      case MassFlowControllerCommand.ChooseDifferentGas:
        var gasNumberParam = parameters.First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.GasNumber}"));

        if(!gasNumberParam.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The MFC command ChooseDifferentGas requires a number value as a parameter, but none was provided!";
          break;
        }

        await Device.ChooseDifferentGas((int)gasNumberParam.Value.NumberValue);
        result.Success = true;
        break;

      case MassFlowControllerCommand.DeleteComposerMix:
        var mixParam = parameters.First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.MixNumber}"));
        if(!mixParam.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The MFC command DeleteComposerMix requires a number value as a parameter, but none was provided!";
          break;
        }

        await Device.DeleteComposerMix((int)mixParam.Value.NumberValue);
        result.Success = true;
        break;

      case MassFlowControllerCommand.HoldValvesAtCurrentPosition:
        await Device.HoldValvesAtCurrentPosition();
        break;

      case MassFlowControllerCommand.HoldValvesClosed:
        await Device.CancelValveHold();
        break;

      case MassFlowControllerCommand.NewComposerMix:
        // TODO: oof, generate composer mix from the parameters
        // Device.NewComposerMix();
        throw new NotImplementedException();

      case MassFlowControllerCommand.NewSetpoint:
        var setpointParameter = parameters.First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.Setpoint}"));
        if(!setpointParameter.Value.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The NewSetpoint command requires a number value as a parameter, but none was provided!";
          break;
        }

        try
        {
          await Device.NewSetpoint(StandardVolumeFlow.FromStandardLitersPerMinute(setpointParameter.Value.NumberValue));
        }
        catch(Exception e)
        {
          result.Success = false;
          result.Error = e.Message;
          break;
        }

        result.Success = true;
        break;

      case MassFlowControllerCommand.GetSetpoint:
        var data = await Device.StateStream.Take(1);
        var setpt = data.LiveData?.Setpoint?.Value;
        if(setpt is not null)
        {
          result.Result = AresStructHelper.CreateNumberStruct(MfcDataTypes.Setpoint.Key, setpt.Value);
        }
        else
        {
          result.Result = AresStructHelper.CreateNullStruct(MfcDataTypes.Setpoint.Key);
        }
        result.Success = true;
        break;

      case MassFlowControllerCommand.TareAbsolutePressureWithBarometer:
        await Device.TareAbsolutePressureWithBarometer();
        break;

      case MassFlowControllerCommand.TareFlow:
        await Device.TareFlow();
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }

    return result;
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    var metadatas = new List<CommandMetadata>
    {
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.NewSetpoint.ToString(),
        Description = "Sets a new target mass flow",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = MassFlowControllerCommandParameter.Setpoint.ToString(),
            Unit = StandardVolumeFlowUnit.StandardLiterPerMinute.ToString(),
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          }
        }
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.ChangeUnitId.ToString(),
        Description = "Assigns the device a new letter ID",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = MassFlowControllerCommandParameter.DeviceId.ToString(),
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.String, true)
          }
        }
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.PollLiveDataFrame.ToString(),
        Description = "Queries the device for a live data entry containing device ID, temperature, flow, setpoint, and gas. Depending on the type of the MFC, it may also include pressure and other data items."
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.CancelValveHold.ToString(),
        Description = "Cancels holds on the device's valve(s)"
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.ChooseDifferentGas.ToString(),
        Description = "Changes the currently managed gas",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = MassFlowControllerCommandParameter.GasNumber.ToString(),
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.String, true)
          }
        }
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.GetSetpoint.ToString(),
        Description = "Gets the current setpoint of the MFC",
        OutputMetadata = new OutputMetadata
        {
          Description = "Current setpoint",
          DataSchema = AresSchemaHelper.CreateSchema(MfcDataTypes.Setpoint.Key, MfcDataTypes.Setpoint.Value)
        }
      }
    };

    if (Device is MassFlowController mfc && mfc.MfcType == Ares.Alicat.Mfc.Config.MfcType.Basis2)
    {
      metadatas.AddRange(new CommandMetadata[]
      {
        new()
        {
          DeviceId = Device.UniqueId,
          Name = MassFlowControllerCommand.HoldValvesClosed.ToString(),
          Description = "Holds the device's valve(s) at the given position",
          ParameterMetadatas = {
            new ParameterMetadata
            {
              Index = 0,
              Name = MassFlowControllerCommandParameter.ValvePercent.ToString(),
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false)
            }
          }
        }
      });
    }

    if(Device is MassFlowController mfc2 && mfc2.MfcType == Ares.Alicat.Mfc.Config.MfcType.Normal)
    {
      metadatas.AddRange(new CommandMetadata[]
      {
        new()
        {
          DeviceId = Device.UniqueId,
          Name = MassFlowControllerCommand.ManufacturerInfo.ToString(),
          Description = "Queries the manufacturer info"
        },
        new()
        {
          DeviceId = Device.UniqueId,
          Name = MassFlowControllerCommand.TareAbsolutePressureWithBarometer.ToString(),
          Description = "Tares the device's absolute pressure with barometer"
        },
        new()
        {
          DeviceId = Device.UniqueId,
          Name = MassFlowControllerCommand.TareFlow.ToString(),
          Description = "Tares the device's flow"
        },      
        new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.HoldValvesAtCurrentPosition.ToString(),
        Description = "Holds the device's valve(s) at the current position"
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.HoldValvesClosed.ToString(),
        Description = "Holds the device's valve(s) at the closed position"
      },
      new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.NewComposerMix.ToString(),
        Description = "Adds a new COMPOSER mix to the device's memory"
        // TODO: ParameterMetadata
      },      
        new()
      {
        DeviceId = Device.UniqueId,
        Name = MassFlowControllerCommand.DeleteComposerMix.ToString(),
        Description = "Deletes the indicated COMPOSER Mix number from the device's memory",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = MassFlowControllerCommandParameter.MixNumber.ToString(),
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.String, true)
          }
        }
      },
      });

    }

    return metadatas.ToArray();
  }
}
