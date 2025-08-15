using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace ValveController;
public class ValveControllerInterpreter : DeviceCommandInterpreter<ValveController, ValveControllerCommand>
{
  public ValveControllerInterpreter(ValveController device) : base(device) { }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.GetRelayStatus.ToString(),
        Description = "Determines the status of the relay board, telling ARES whether the relays are currently engaged or disengaged.",
        OutputMetadata = new OutputMetadata()
        {
          Description = "Returns the current engagement status of the relay channels.",
          DataSchema = AresSchemaHelper.CreateSchema("Relay1", AresDataType.Boolean).AddEntry("Relay2", AresDataType.Boolean),
          Index = 0
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.EngageRelayOne.ToString(),
        Description = "Set's the device attached to the valve controllers first relay to the engaged state."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.EngageRelayTwo.ToString(),
        Description = "Set's the device attached to the valve controllers second relay to the engaged state."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.DisengageRelayOne.ToString(),
        Description = "Set's the device attached to the valve controllers first relay to the disengaged state."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.DisengageRelayTwo.ToString(),
        Description = "Set's the device attached to the valve controllers second relay to the disengaged state."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ValveControllerCommand.EnableRelays.ToString(),
        Description = "Ensures that the Valve Controller's Relay's are enabled and ready for operation."
      }

    };
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(ValveControllerCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    switch(deviceCommandEnum)
    {
      case ValveControllerCommand.GetRelayStatus:
        var data = await Device.GetRelayStatus();
        var result = new CommandResult
        {
          Result = AresStructHelper.CreateBoolStruct("Relay1", data.RelayOneOn).AddBool("Relay2", data.RelayTwoOn),
          Success = true
        };

        return result;

      case ValveControllerCommand.EngageRelayOne:
        await Device.EngageRelayOne();
        return new CommandResult() { Success = true };

      case ValveControllerCommand.EngageRelayTwo:
        await Device.EngageRelayTwo();
        return new CommandResult { Success = true };

      case ValveControllerCommand.DisengageRelayOne:
        await Device.DisengageRelayOne();
        return new CommandResult { Success = true };

      case ValveControllerCommand.DisengageRelayTwo:
        await Device.DisengageRelayTwo();
        return new CommandResult() { Success = true };

      case ValveControllerCommand.EnableRelays:
        await Device.EnableRelays();
        return new CommandResult() { Success = true };

      default:
        throw new InvalidOperationException("Received an unknown command for the valve controller!");
    }
  }
}
