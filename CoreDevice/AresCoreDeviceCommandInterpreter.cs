using Ares.Device;
using Ares.Messaging;
using UnitsNet;
using UnitsNet.Units;

namespace CoreDevice;

public class AresCoreDeviceCommandInterpreter : DeviceCommandInterpreter<AresCoreDevice, AresCoreDeviceCommand>
{
  public AresCoreDeviceCommandInterpreter(AresCoreDevice device) : base(device)
  { }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
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
              Unit = DurationUnit.Millisecond.ToString()
            }
          }
      }
    };
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(AresCoreDeviceCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();
    switch (deviceCommandEnum)
    {
      case AresCoreDeviceCommand.Sleep:
        var durationParam = parameters[0];
        var duration = Duration.FromMilliseconds(durationParam.Value.Value);
        await Device.Sleep(duration.ToTimeSpan());
        result.Success = true;
        return result;

      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }
}
