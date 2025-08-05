using Ares.Device;
using Ares.Messaging;
using Ares.Tools;
using Google.Protobuf.WellKnownTypes;
using RestDevice.Enums;
using RestDevice.Generics;
using RestDevice.Structure;

namespace RestDevice;

public class RestDeviceInterpreter : DeviceCommandInterpreter<IRestDevice, RestDeviceCommandEnum>
{
  public RestDeviceInterpreter(IRestDevice device) : base(device)
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
        DeviceName = Device.Name,
        UniqueId = function.UniqueId
      };

      if(function.Parameters.Any())
        cmdMetadata.ParameterMetadatas.AddRange(GenerateParameterMetadatas(function.Parameters));

      if(function.Output.Any())
        cmdMetadata.OutputMetadata = GenerateOutputMetadata(function.Output.First());

      metadata.Add(cmdMetadata);
    }

    return metadata.ToArray();
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(RestDeviceCommandEnum deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();

    switch(deviceCommandEnum)
    {
      case RestDeviceCommandEnum.None:
        //Custom Device Command
        var stringParameters = CreateParameterList(parameters);
        //TODO: FIX ME!!
        var response = await Device.ProcessCommand(metadata.DeviceName, stringParameters, stringParameters);
        result.Result.AddValue("result", response);
        break;

      default:
        throw new InvalidOperationException();
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
          UniqueId = parameter.UniqueId
        };

        metadata.Constraints.Add(limit);
      }

      parameterMetadatas.Add(metadata);
      index++;
    }

    return parameterMetadatas;
  }

  private OutputMetadata GenerateOutputMetadata(RestDeviceOutput output)
  {
    //TODO: Handle multiple outputs?
    var outputMetadata = new OutputMetadata()
    {
      DataSchema = AresSchemaHelper.CreateSchema(output.Name, ConvertGenericTypeToAresType(output.Type)),
      Description = output.Description,
      Index = 0,
      UniqueId = output.UniqueId
    };

    return outputMetadata;
  }

  private List<string> CreateParameterList(Parameter[] parameters)
  {
    var paramList = new List<string>();

    foreach(var parameter in parameters)
    {
      if(!parameter.Value.Value.HasStringValue)
        continue;
      
      //TODO: VALIDATE AND FIX AHHH
      paramList.Add(parameter.Value.Value.StringValue);
    }

    return paramList;
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
