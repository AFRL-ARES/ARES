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

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(MassFlowControllerCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

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

        await Device.NewSetpoint(StandardVolumeFlow.FromStandardCubicCentimetersPerMinute(setpointParameter.Value.NumberValue));
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
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.NewSetpoint.ToString(),
        Description = "Sets a new target mass flow",
        ParameterMetadatas =
        {
          new ParameterMetadata
          {
            Index = 0,
            Name = MassFlowControllerCommandParameter.Setpoint.ToString(),
            Unit = MassFlowUnit.CentigramPerSecond.ToString(), // TODO: Verify unit
            Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true)
          }
        }
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.ManufacturerInfo.ToString(),
        Description = "Queries the manufacturer info"
      },
      new()
      {
        DeviceName = Device.Name,
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
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.PollLiveDataFrame.ToString(),
        Description = "Queries the device for a live data entry containing device ID, absolute pressure, temperature, volumetric flow, mass flow, setpoint, and gas"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.CancelValveHold.ToString(),
        Description = "Cancels holds on the device's valve(s)"
      },
      new()
      {
        DeviceName = Device.Name,
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
        DeviceName = Device.Name,
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
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.HoldValvesAtCurrentPosition.ToString(),
        Description = "Holds the device's valve(s) at the current position"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.HoldValvesClosed.ToString(),
        Description = "Holds the device's valve(s) at the closed position"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.NewComposerMix.ToString(),
        Description = "Adds a new COMPOSER mix to the device's memory"
        // TODO: ParameterMetadata
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.TareAbsolutePressureWithBarometer.ToString(),
        Description = "Tares the device's absolute pressure with barometer"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.TareFlow.ToString(),
        Description = "Tares the device's flow"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.GetSetpoint.ToString(),
        Description = "Gets the current setpoint of the MFC",
        OutputMetadata = new OutputMetadata
        {
          Description = "Current setpoint",
          DataSchema = AresSchemaHelper.CreateSchema(MfcDataTypes.Setpoint.Key, MfcDataTypes.Setpoint.Value)
        }
      }
    };
  }
}
