using Ares.Device;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using TC0304.Commands;
using Tc0304.DataModel;
using TC0304.Extensions;

namespace TC0304;

public class DataLoggerThermometerInterpreter : DeviceCommandInterpreter<DataloggerThermometer, DataLoggerCommand>
{
  public DataLoggerThermometerInterpreter(DataloggerThermometer device) : base(device)
  {
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(DataLoggerCommand deviceCommandEnum, Parameter[] parameters, CancellationToken cancellationToken)
  {
    switch (deviceCommandEnum)
    {
      case DataLoggerCommand.GetData:
        var data = await Device.GetAndUpdateState();
        var result = new DeviceCommandResult
        {
          Result = Any.Pack(data.ToProto()),
          Success = true
        };

        return result;
      case DataLoggerCommand.Hold:
        Device.Hold();
        return new DeviceCommandResult { Success = true };
      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    var metadata = new List<CommandMetadata>();
    metadata.Add(new CommandMetadata
    {
      Name = DataLoggerCommand.GetData.ToString(),
      Description = "Gets the most recent data for the device",
      DeviceName = Device.Name,
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = new OutputMetadata
      {
        Description = "The most recent data for the data logger",
        FullName = typeof(Data).FullName,
        UniqueId = Guid.NewGuid().ToString()
      }
    });

    metadata.Add(new CommandMetadata
    {
      Name = DataLoggerCommand.Hold.ToString(),
      Description = "Holds the current temperature reading",
      DeviceName = Device.Name,
      UniqueId = Guid.NewGuid().ToString()
    });

    return metadata.ToArray();
  }
}
