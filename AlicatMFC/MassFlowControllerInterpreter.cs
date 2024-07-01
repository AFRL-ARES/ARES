using AlicatMFC.Commands;
using Ares.Device;
using Ares.Messaging;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFC;

public class MassFlowControllerInterpreter : DeviceCommandInterpreter<IMassFlowController, MassFlowControllerCommand>
{
  public MassFlowControllerInterpreter(IMassFlowController device) : base(device)
  {
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(MassFlowControllerCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch (deviceCommandEnum)
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
        var gasNumber =
          parameters
            .First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.GasNumber}"))
            .Value.Value;

        await Device.ChooseDifferentGas((int)gasNumber);
        break;
      case MassFlowControllerCommand.DeleteComposerMix:
        var mixNumber =
          parameters
            .First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.MixNumber}"))
            .Value.Value;

        Device.DeleteComposerMix((int)mixNumber);
        break;
      case MassFlowControllerCommand.HoldValvesAtCurrentPosition:
        Device.HoldValvesAtCurrentPosition();
        break;
      case MassFlowControllerCommand.HoldValvesClosed:
        Device.CancelValveHold();
        break;
      case MassFlowControllerCommand.NewComposerMix:
        // TODO: oof, generate composer mix from the parameters
        // Device.NewComposerMix();
        throw new NotImplementedException();

        break;
      case MassFlowControllerCommand.NewSetpoint:
        var setpointParameter =
          parameters
            .First(param => param.Metadata.Name.Equals($"{MassFlowControllerCommandParameter.Setpoint}"));

        // TODO: Do Units.Net magic with the setpointParameter.Metadata.Unit rather than hardcode the units
        throw new NotImplementedException();

        var setpoint = StandardVolumeFlow.FromStandardCubicCentimetersPerMinute(setpointParameter.Value.Value);
        Device.NewSetpoint(setpoint);
        break;
      case MassFlowControllerCommand.TareAbsolutePressureWithBarometer:
        Device.TareAbsolutePressureWithBarometer();
        break;
      case MassFlowControllerCommand.TareFlow:
        Device.TareFlow();
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
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = MassFlowControllerCommandParameter.Setpoint.ToString(), Unit = MassFlowUnit.CentigramPerSecond.ToString() } }// TODO: Verify unit
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
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = MassFlowControllerCommandParameter.DeviceId.ToString() } }
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
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = MassFlowControllerCommandParameter.GasNumber.ToString() } }
      },
      new()
      {
        DeviceName = Device.Name,
        Name = MassFlowControllerCommand.DeleteComposerMix.ToString(),
        Description = "Deletes the indicated COMPOSER Mix number from the device's memory",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = MassFlowControllerCommandParameter.MixNumber.ToString() } }
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
      }
    };
  }
}
