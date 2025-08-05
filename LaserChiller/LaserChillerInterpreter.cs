using Ares.Device;
using Ares.Messaging;
using Ares.Tools;
using Google.Protobuf.WellKnownTypes;
using LaserChiller.Extensions;

namespace LaserChiller;

public class LaserChillerInterpreter : DeviceCommandInterpreter<LaserChiller, LaserChillerCommand>
{
  public LaserChillerInterpreter(LaserChiller device) : base(device)
  {

  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = LaserChillerCommand.SetStabilizedTemperature.ToString(),
        Description = "Set the temperature which the laser chiller will attempt to reach.",
        ParameterMetadatas = { new ParameterMetadata {  Index = 0, Name = LaserChillerCommandParameter.TargetTemperature.ToString(), Unit = "Degrees Celsius" } }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = LaserChillerCommand.SetChillerRunMode.ToString(),
        Description = "Set's the chiller to run mode, which starts the cooling process to achieve the current target temperature."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = LaserChillerCommand.SetChillerStandbyMode.ToString(),
        Description = "Set's the chiller to standby mode."
      }
    };
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(LaserChillerCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch(deviceCommandEnum)
    {
      case LaserChillerCommand.SetStabilizedTemperature:
        var desiredTemp = parameters.First(param => param.Metadata.Name.Equals($"{LaserChillerCommandParameter.TargetTemperature}"));

        if(desiredTemp.Value.Value.HasNumberValue)
          await Device.SetStabilizedTemperature(desiredTemp.Value.Value.NumberValue);

        break;

      case LaserChillerCommand.SetChillerRunMode:
        await Device.SetChillerRunMode();
        result.Success = true;
        break;

      case LaserChillerCommand.SetChillerStandbyMode:
        await Device.SetChillerStandbyMode();
        result.Success = true;
        break;

      case LaserChillerCommand.UpdateManifoldTemperature:
        try
        {
          var data = await Device.GetAndUpdateState();
          result.Success = true;
          result.Result = AresStructHelper.CreateNumberStruct("ManifoldTemperature", data.Temperature);
        }

        catch(Exception ex)
        {
          result.Success = false;
          result.Error = ex.Message;
        }

        break;

      default:
        result.Success = false;
        result.Error = "Unknown Laser Chiller command received!";
        break;
    }

    return result;
  }
}
