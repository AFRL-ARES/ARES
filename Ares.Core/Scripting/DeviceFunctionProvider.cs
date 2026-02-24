using System.Text;
using Ares.Core.Device.Repos;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
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
        var parameterMetadatas = descriptor.InputSchema;

        var inputSchema = descriptor.InputSchema;

        var outputSchema = descriptor.OutputSchema is null
          ? AresSchemaBuilder.Entry(AresDataType.Unit).Build()
          : descriptor.OutputSchema;

        functions.Add(
          new AresSystemFunction(
            functionId,
            commandName,
            async (args, token) =>
            {
              if(args.Count > inputSchema.Fields.Count)
              {
                throw new InvalidOperationException(
                  $"Function '{functionId}' expected at most {inputSchema.Fields.Count} arguments but got {args.Count}.");
              }

              var deviceArgs = args.Select(kvp => new DeviceCommandArgument() { ArgName = kvp.Key, ArgValue = kvp.Value });

              var result = await device.ExecuteCommand(commandName, deviceArgs.ToList(), token.CancellationToken);

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
