using Ares.Device;
using Ares.Datamodel;
using UnitsNet.Units;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Extensions;

namespace Ares.Core.CoreDevice;

public class AresCoreDeviceCommandInterpreter : DeviceCommandInterpreter<AresCoreDevice, AresCoreDeviceCommand>
{
  public AresCoreDeviceCommandInterpreter(AresCoreDevice device) : base(device)
  { }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return
    [
      new CommandMetadata
      {
        DeviceName = Device.Name,
        Name = AresCoreDeviceCommand.Sleep.ToString(),
        Description = "Sleep for a given amount of time.",
        ParameterMetadatas =
          {
            new ParameterMetadata
            {
              Name = AresCoreDeviceCommandParameter.Duration.ToString(),
              Index = 0,
              Unit = $"{DurationUnit.Millisecond}s",
              Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false)
            }
          }
      },

      new CommandMetadata
      {
        DeviceName = Device.Name,
        Name = AresCoreDeviceCommand.WaitForUser.ToString(),
        Description = "ARES will request user confirmation before continuing."
      }
    ];
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(AresCoreDeviceCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();
    switch(deviceCommandEnum)
    {
      case AresCoreDeviceCommand.Sleep:
        var durationParam = parameters[0];

        var duration = UnitsNet.Duration.FromMilliseconds(durationParam.Value.NumberValue);
        await Device.Sleep(duration.ToTimeSpan());
        result.Success = true;
        return result;

      case AresCoreDeviceCommand.WaitForUser:
        result.Success = true;
        result.AwaitUserInput = true;
        return result;

      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }
}
