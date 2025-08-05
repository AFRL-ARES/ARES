using Ares.Device;
using Ares.Messaging;
using Ares.Tools;
using TC0304.Commands;
using Tc0304.DataModel;

namespace TC0304;

public class DataLoggerThermometerInterpreter : DeviceCommandInterpreter<DataloggerThermometer, DataLoggerCommand>
{
  public DataLoggerThermometerInterpreter(DataloggerThermometer device) : base(device)
  {
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(DataLoggerCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch(deviceCommandEnum)
    {
      case DataLoggerCommand.GetData:
        var data = await Device.GetAndUpdateState();
        result.Success = true;
        result.Result = AresStructHelper.CreateNullStruct("LoggerData");
        return result;

      case DataLoggerCommand.GetTemperatures:
        var temp_data = await Device.GetTemperatures();
        result.Success = true;
        result.Result = AresStructHelper.CreateNumberArrayStruct("TempData", temp_data);
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

    //Special case, multiple unique probes requires multiple schema entries.
    var getTempMetadata = new CommandMetadata
    {
      Name = DataLoggerCommand.GetTemperatures.ToString(),
      Description = "Gets the most recent temperatures from the data logger service.",
      DeviceName = Device.Name,
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = new OutputMetadata
      {
        Description = "The most recent data for the data logger",
        DataSchema = new AresDataSchemaSimplified(),
        UniqueId = Guid.NewGuid().ToString()
      }
    };

    getTempMetadata.OutputMetadata.DataSchema.AddEntry("T1Probe", AresDataType.Number);
    getTempMetadata.OutputMetadata.DataSchema.AddEntry("T2Probe", AresDataType.Number);
    getTempMetadata.OutputMetadata.DataSchema.AddEntry("T3Probe", AresDataType.Number);
    getTempMetadata.OutputMetadata.DataSchema.AddEntry("T4Probe", AresDataType.Number);
    metadata.Add(getTempMetadata);

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
