using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;

namespace Ares.Core.Device.Remote;
internal static class CommandHelpers
{
  public static CommandMetadata[] ToCommandMetadata(IEnumerable<DeviceCommandDescriptor> deviceCommandDescriptors, string deviceName)
  {
    return [.. deviceCommandDescriptors.Select(dcd => dcd.ToCommandMetadata(deviceName))];
  }

  public static CommandMetadata ToCommandMetadata(this DeviceCommandDescriptor deviceCommandDescriptor, string deviceName)
  {
    var metadata = new CommandMetadata
    {
      Name = deviceCommandDescriptor.Name,
      Description = deviceCommandDescriptor.Description,
      DeviceName = deviceName,
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = deviceCommandDescriptor.ToOutputMetadata()
    };
    metadata.ParameterMetadatas.AddRange(deviceCommandDescriptor.ToParameterMetadatas());

    return metadata;
  }

  public static OutputMetadata? ToOutputMetadata(this DeviceCommandDescriptor deviceCommandDescriptor)
  {
    if(deviceCommandDescriptor.OutputSchema is null)
    {
      return null;
    }

    var metadata = new OutputMetadata
    {
      DataSchema = deviceCommandDescriptor.OutputSchema
    };

    return metadata;
  }

  public static ParameterMetadata[] ToParameterMetadatas(this DeviceCommandDescriptor deviceCommandDescriptor)
  {
    if(deviceCommandDescriptor.InputSchema is null)
    {
      return [];
    }

    return deviceCommandDescriptor.InputSchema.Fields
      .Select(
        f => new ParameterMetadata { Name = f.Key, Schema = f.Value }).ToArray();
  }

  public static AresStruct ParametersToStruct(IEnumerable<Parameter> parameters)
  {
    var aresStruct = new AresStruct();

    foreach(var parameter in parameters)
    {
      aresStruct.Fields[parameter.Metadata.Name] = parameter.Value;
    }

    return aresStruct;
  }
}
