using Ares.Device;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using HerkulexDRS.Extensions;

namespace HerkulexDRS;
public class ServoInterpreter : DeviceCommandInterpreter<Servo, ServoCommand>
{
  public ServoInterpreter(Servo device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    throw new NotImplementedException();
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(ServoCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    switch (deviceCommandEnum)
    {
      case ServoCommand.GetPosition:
        var data = await Device.GetPosition();
        var result = new DeviceCommandResult
        {
          Result = Any.Pack(data.ToProto()),
          Success = true
        };

        return result;

      default: throw new NotImplementedException();
    }
  }
}
