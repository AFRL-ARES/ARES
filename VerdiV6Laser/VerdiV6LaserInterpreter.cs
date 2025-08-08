using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Device;

namespace VerdiV6Laser
{
  public class VerdiV6LaserInterpreter : DeviceCommandInterpreter<VerdiV6Laser, VerdiV6LaserCommand>
  {
    public VerdiV6LaserInterpreter(VerdiV6Laser device) : base(device)
    {

    }

    protected override CommandMetadata[] CommandsToMetadatas()
    {
      return new CommandMetadata[]
      {
        new()
        {
          DeviceName = Device.Name,
          Name = VerdiV6LaserCommand.SetShutter.ToString(),
          Description = "Set the shutter of the laser to on or off.",
          ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = VerdiV6LaserCommandParameter.Shutter.ToString(), Unit = "On/Off" } }
        },
        new()
        {
          DeviceName = Device.Name,
          Name = VerdiV6LaserCommand.SetPower.ToString(),
          Description = "Set the power of the laser.",
          ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = VerdiV6LaserCommandParameter.LaserPower.ToString(), Unit = "Laser Power" } }
        },
        new()
        {
          DeviceName = Device.Name,
          Name = VerdiV6LaserCommand.ActivateLaser.ToString(),
          Description = "CAUTION! Will activate the laser utilizing whatever power was last set as the desired active power. Proceed with caution!"
        },
        new()
        {
          DeviceName = Device.Name,
          Name = VerdiV6LaserCommand.DeactivateLaser.ToString(),
          Description = "Set's the Verdi V6 Laser back to minimal power levels, deactivating the laser."
        }
      };
    }

    protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(VerdiV6LaserCommand deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
    {
      var result = new DeviceCommandResult();

      switch(deviceCommandEnum)
      {
        case VerdiV6LaserCommand.SetPower:
          var power = parameters.First(param => param.Metadata.Name.Equals($"{VerdiV6LaserCommandParameter.LaserPower}")).Value.Value;
          await Device.SetLaserPower(power.NumberValue);
          result.Success = true;
          break;

        case VerdiV6LaserCommand.SetShutter:
          var shutter = parameters.First(param => param.Metadata.Name.Equals($"{VerdiV6LaserCommandParameter.Shutter}")).Value.Value;
          await Device.SetLaserShutter(shutter.NumberValue == 1);
          break;

        case VerdiV6LaserCommand.ActivateLaser:
          await Device.ActivateLaser();
          break;

        case VerdiV6LaserCommand.DeactivateLaser:
          await Device.DeactivateLaser();
          break;
      }

      return result;
    }
  }
}
