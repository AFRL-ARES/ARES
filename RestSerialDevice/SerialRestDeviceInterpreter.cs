using Ares.Datamodel;
using Ares.Device;
using RestSerialDevice.Enums;
using RestSerialDevice.Structure;
using RestSerialDevice.Generics;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace RestSerialDevice;

public class SerialRestDeviceInterpreter : DeviceCommandInterpreter<SerialRestDevice, SerialRestDeviceCommandsEnum>
{
  public SerialRestDeviceInterpreter(SerialRestDevice device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    var metadata = new List<CommandMetadata>();

    foreach(var function in Device.Functions)
    {
      var cmdMetadata = new CommandMetadata()
      {
        Name = function.Name,
        Description = function.Description,
        DeviceName = Device.Name
      };

      if(function.Parameters.Any())
        cmdMetadata.ParameterMetadatas.AddRange(GenerateParameterMetadatas(function.Parameters));

      if(function.Output.Any())
        cmdMetadata.OutputMetadata = GenerateOutputMetadata(function.Output.First());

      metadata.Add(cmdMetadata);
    }

    return metadata.ToArray();

  }
  
  private OutputMetadata GenerateOutputMetadata(RestSerialDeviceOutput output)
  {
    //TODO: Handle multiple outputs?
    var outputMetadata = new OutputMetadata()
    {
      DataSchema = AresSchemaHelper.CreateSchema(output.Name, ConvertGenericTypeToAresType(output.Type)),
      Description = output.Description,
      Index = 0,
    };

    return outputMetadata;
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(SerialRestDeviceCommandsEnum deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch(deviceCommandEnum)
    {
      case SerialRestDeviceCommandsEnum.GetData:
        var data = await Device.GetAndUpdateState();
        result.Result = new AresStruct();

        foreach(var val in data.Values)
          result.Result.AddString(val.Key, val.Value);

        result.Success = true;
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(deviceCommandEnum), deviceCommandEnum, null);
    }

    return result;
  }

  private List<ParameterMetadata> GenerateParameterMetadatas(List<RestDeviceParameter> parameters)
  {
    var parameterMetadatas = new List<ParameterMetadata>();
    var index = 0;
    foreach(var parameter in parameters)
    {
      var metadata = new ParameterMetadata()
      {
        Name = parameter.Name,
        Index = index,
        Unit = parameter.Unit,
        UniqueId = Guid.NewGuid().ToString()
      };

      if(parameter.Minimum is not null && parameter.Maximum is not null)
      {
        var limit = new Limits()
        {
          Index = 0,
          Minimum = (float)parameter.Minimum,
          Maximum = (float)parameter.Maximum,
          UniqueId = Guid.NewGuid().ToString()
        };

        metadata.Constraints.Add(limit);
      }

      parameterMetadatas.Add(metadata);
      index++;
    }

    return parameterMetadatas;
  }

  private AresDataType ConvertGenericTypeToAresType(System.Type type)
  {
    if(type == typeof(string))
      return AresDataType.String;

    if(type == typeof(double) || type == typeof(int) || type == typeof(float))
      return AresDataType.Number;


    throw new InvalidOperationException();
  }
}
