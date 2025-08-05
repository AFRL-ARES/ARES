using Ares.Device;
using Ares.Messaging;
using Ares.Tools;

namespace HerkulexDRS;
public class ServoInterpreter : DeviceCommandInterpreter<Servo, ServoCommand>
{
  public ServoInterpreter(Servo device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = ServoCommand.Reset.ToString(),
        Description = "This command will force the servo to re-initalize its own state. The servo will glow red when in a broken state. " +
        "Reseting the servo allows for normal operation to resume when an error is encountered."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ServoCommand.GetPosition.ToString(),
        Description = "This command is used to determine the starting position of the servo device. This is mostly for internal usage, rather than for an ARES end user.",
        OutputMetadata = new OutputMetadata()
        {
          Description = "Servo Position",
          DataSchema = AresSchemaHelper.CreateSchema("ServoPosition", AresDataType.Number)
        }
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ServoCommand.GoUp.ToString(),
        Description = "This command will move the servo to it's upward (closed) position."
      },

      new()
      {
        DeviceName = Device.Name,
        Name = ServoCommand.GoDown.ToString(),
        Description = "This command will move the servo to it's downward (open) position."
      }

    };
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(ServoCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    DeviceCommandResult result;

    switch(deviceCommandEnum)
    {
      case ServoCommand.GetPosition:
        var data = await Device.GetPosition();
        return result = new DeviceCommandResult { Result = AresStructHelper.CreateNumberStruct("ServoPosition", data.Position), Success = true };

      case ServoCommand.GoUp:
        await Device.ResetServo();
        Thread.Sleep(TimeSpan.FromSeconds(4));
        await Device.PistonUp();
        return new DeviceCommandResult { Success = true };

      case ServoCommand.GoDown:
        await Device.ResetServo();
        Thread.Sleep(TimeSpan.FromSeconds(4));
        await Device.PistonDown();
        return new DeviceCommandResult { Success = true };

      case ServoCommand.Reset:
        await Device.ResetServo();
        return new DeviceCommandResult { Success = true };

      default:
        throw new NotSupportedException("Received a servo command that was not supported.");
    }
  }
}
