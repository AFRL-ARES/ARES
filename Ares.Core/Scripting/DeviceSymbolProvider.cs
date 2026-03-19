using Ares.Core.Device.Providers;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using Ares.Datamodel.Templates;
using AresScript.Symbols;
using System.Text;

namespace Ares.Core.Scripting;

public class DeviceSymbolProvider(IAresDeviceProvider deviceProvider) : ISymbolProvider
{
  private readonly IAresDeviceProvider _deviceProvider = deviceProvider;


  public IScriptSymbol[] GetSymbols()
  {
    var devices = _deviceProvider.GetAllDevices();
    var symbols = new List<IScriptSymbol>();

    foreach(var device in devices)
    {
      var devicePrefix = SanitizeIdentifier(string.IsNullOrWhiteSpace(device.Name) ? device.UniqueId : device.Name);
      var descriptors = device.GetCommandDescriptorsAsync().GetAwaiter().GetResult();
      var deviceFunctionFields = new Dictionary<string, AresSystemFunctionSymbol>(StringComparer.Ordinal);

      foreach(var descriptor in descriptors)
      {
        var commandName = SanitizeIdentifier(descriptor.Name);
        var functionId = $"devices::{devicePrefix}::{commandName}";

        var outputSchema = descriptor.OutputSchema ?? AresSchemaBuilder.Entry(AresDataType.Unit).Build();
        var inputSchema = descriptor.InputSchema ?? new AresStructSchema();

        var functionSymbol = new AresSystemFunctionSymbol(
          functionId,
          commandName,
          async (args, token) =>
          {
            if(args.Count > (descriptor.InputSchema?.Fields.Count ?? 0))
            {
              throw new InvalidOperationException(
                $"Function '{functionId}' expected at most {descriptor.InputSchema?.Fields.Count ?? 0} arguments but got {args.Count}.");
            }

            var result = await device.ExecuteCommand(commandName, args.Select(arg => new Datamodel.Device.DeviceCommandArgument { ArgName = Guid.NewGuid().ToString(), ArgValue = arg }).ToList(), token.CancellationToken);
            if(!result.Success)
            {
              throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.Error)
                  ? $"Device command '{descriptor.Name}' failed."
                  : result.Error);
            }

            return result.Result ?? AresValueHelper.CreateUnit();
          },
          inputSchema,
          outputSchema,
          Namespace: string.Empty,
          ParentName: $"devices.{devicePrefix}")
        {
          Documentation = descriptor.Description
        };

        symbols.Add(functionSymbol);
        deviceFunctionFields[commandName] = functionSymbol;
      }

      symbols.Add(AresSystemValue.Struct(
          CreateDeviceStruct(deviceFunctionFields.Values).StructValue,
          device.Name,
          SymbolKind.Device)
        with
      {
        Name = devicePrefix,
        ParentName = "devices",
        Detail = device.Type,
        Documentation = device.Description,
        IsReadOnly = true
      });
    }

    return symbols.ToArray();
  }

  private static AresValue CreateDeviceStruct(ICollection<AresSystemFunctionSymbol> symbols)
  {
    var structVal = AresValueHelper.CreateStruct();

    foreach(var symbol in symbols)
    {
      structVal.StructValue.Fields[symbol.Name] = AresValueHelper.CreateFunction(symbol.Id);
    }

    return structVal;
  }

  private static AresStructSchema BuildInputSchema(IEnumerable<ParameterMetadata> parameterMetadatas)
  {
    var schema = new AresStructSchema();
    foreach(var parameterMetadata in parameterMetadatas)
    {
      var entry = parameterMetadata.Schema ?? new AresValueSchema { Type = AresDataType.Any, Optional = true };
      schema.Fields[parameterMetadata.Name] = entry;
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
