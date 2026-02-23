using System.Text;
using Ares.Core.Device;
using Ares.Core.Device.Repos;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;
using AresScript;

namespace Ares.Core.Scripting;

public class DeviceFunctionProvider(IAresDeviceRepo deviceRepo) : ISystemFunctionProvider
{
  private readonly IAresDeviceRepo _deviceRepo = deviceRepo;

  public AresSystemFunction[] GetFunctions()
  {
    var devices = _deviceRepo.GetSnapshot();
    var functions = new List<AresSystemFunction>();

    foreach(var device in devices)
    {
      var devicePrefix = SanitizeIdentifier(string.IsNullOrWhiteSpace(device.Name) ? device.UniqueId : device.Name);
      var commandDescriptors = device.CommandDescriptors.ToArray();

      foreach(var descriptor in commandDescriptors)
      {
        var commandName = SanitizeIdentifier(descriptor.Name);
        var functionId = $"devices::{devicePrefix}::{commandName}";
        var parameterMetadatas = descriptor.InputSchema.Fields.ToArray();

        var inputSchema = BuildInputSchema(parameterMetadatas);

        var outputSchema = descriptor.OutputSchema is null
          ? AresSchemaBuilder.Entry(AresDataType.Unit).Build()
          : AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(descriptor.OutputSchema).Build();

        functions.Add(
          new AresSystemFunction(
            functionId,
            commandName,
            async (args, token) =>
            {
              if(args.Count > parameterMetadatas.Length)
              {
                throw new InvalidOperationException(
                  $"Function '{functionId}' expected at most {parameterMetadatas.Length} arguments but got {args.Count}.");
              }

              var template = new CommandTemplate { Metadata = descriptor };
              for(var i = 0; i < args.Count; i++)
              {
                var parameterMetadata = parameterMetadatas[i];
                template.Parameters.Add(
                  new Parameter
                  {
                    Metadata = parameterMetadata,
                    Value = args[i],
                    Index = parameterMetadata.Index
                  });
              }

              var command = device.TemplateToDeviceCommand(template);
              var result = await command(token.CancellationToken);
              if(!result.Success)
              {
                throw new InvalidOperationException(
                  string.IsNullOrWhiteSpace(result.Error)
                    ? $"Device command '{descriptor.Name}' failed."
                    : result.Error);
              }

              return result.Result is null
                ? AresValueHelper.CreateUnit()
                : AresValueHelper.CreateStruct(result.Result);
            },
            inputSchema,
            outputSchema,
            devicePrefix,
            descriptor.Description ?? string.Empty
            ));
      }
    }

    return functions.ToArray();
  }

  private static AresDataSchema BuildInputSchema(IEnumerable<KeyValuePair<string, AresDataSchema>> commandSchemas)
  {
    var schema = new AresDataSchema();
    foreach(var schema in commandSchemas)
    {
      var entry = new SchemaEntry { Type = , Optional = true };
      schema.Fields[schema.Name] = entry;
    }

    return schema;
  }

  private static string SanitizeIdentifier(string value)
  {
    if(string.IsNullOrWhiteSpace(value))
    {
      return "_";
    }

    var builder = new StringBuilder(value.Length);
    foreach(var ch in value)
    {
      builder.Append(IsAsciiIdentifierChar(ch) ? ch : '_');
    }

    if(builder.Length == 0 || !IsAsciiIdentifierStart(builder[0]))
    {
      builder.Insert(0, '_');
    }

    return builder.ToString();
  }

  private static bool IsAsciiIdentifierChar(char value)
  {
    return IsAsciiIdentifierStart(value) || value is >= '0' and <= '9';
  }

  private static bool IsAsciiIdentifierStart(char value)
  {
    return value is >= 'a' and <= 'z'
      or (>= 'A' and <= 'Z')
      or '_';
  }
}
