using Ares.Device;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using ValveController.Extensions;

namespace ValveController;
public class ValveControllerInterpreter : DeviceCommandInterpreter<ValveController, ValveControllerCommand>
{
  public ValveControllerInterpreter(ValveController device) : base(device)
  {

  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    throw new NotImplementedException();
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(ValveControllerCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    switch (deviceCommandEnum)
    {
      case ValveControllerCommand.GetRelayStatus:
        var data = await Device.GetRelayStatus();
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
