using System.Text;
using Ares.Core.Device;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using Ares.Datamodel.Templates;
using AresScript;
using AresScript.Symbols;

namespace Ares.Core.Scripting;

public class DeviceFunctionProvider(IDeviceCommandInterpreterRepo interpreterRepo) : ISymbolProvider
{
  private readonly IDeviceCommandInterpreterRepo _interpreterRepo = interpreterRepo;


  public IScriptSymbol[] GetSymbols()
  {
    var interpreters = _interpreterRepo.GetSnapshot();
    var symbols = new List<IScriptSymbol>();

    foreach(var interpreter in interpreters)
    {
      var device = interpreter.Device;
      var devicePrefix = SanitizeIdentifier(string.IsNullOrWhiteSpace(device.Name) ? device.UniqueId : device.Name);
      var commandMetadatas = interpreter.CommandsToIndexedMetadatas().ToArray();
      var deviceFunctionFields = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);

      foreach(var metadata in commandMetadatas)
      {
        var commandName = SanitizeIdentifier(metadata.Name);
        var functionId = $"devices::{devicePrefix}::{commandName}";
        var parameterMetadatas = metadata.ParameterMetadatas.OrderBy(p => p.Index).ToArray();

        var inputSchema = BuildInputSchema(parameterMetadatas);

        var outputSchema = metadata.OutputMetadata?.DataSchema is null
          ? AresSchemaBuilder.Entry(AresDataType.Unit).Build()
          : AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(metadata.OutputMetadata.DataSchema).Build();

        var functionSymbol = new AresSystemFunction(
          functionId,
          commandName,
          async (args, token) =>
          {
            if(args.Count > parameterMetadatas.Length)
            {
              throw new InvalidOperationException(
                $"Function '{functionId}' expected at most {parameterMetadatas.Length} arguments but got {args.Count}.");
            }

            var template = new CommandTemplate { Metadata = metadata };
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

            var command = interpreter.TemplateToDeviceCommand(template);
            var result = await command(token.CancellationToken);
            if(!result.Success)
            {
              throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.Error)
                  ? $"Device command '{metadata.Name}' failed."
                  : result.Error);
            }

            return result.Result is null
              ? AresValueHelper.CreateUnit()
              : AresValueHelper.CreateStruct(result.Result);
          },
          inputSchema,
          outputSchema,
          Namespace: string.Empty,
          Description: metadata.Description ?? string.Empty,
          ParentName: $"devices.{devicePrefix}");

        symbols.Add(functionSymbol);
        deviceFunctionFields[commandName] = AresSystemValue.Function(functionSymbol);
      }

      symbols.Add(new AresSystemValueSymbol(
        Name: devicePrefix,
        SystemValue: AresSystemValue.Struct(deviceFunctionFields, device.Name, AresSystemValue.AresSystemStructKind.Device),
        Kind: SymbolKind.Device,
        ParentName: "devices"));
    }

    return symbols.ToArray();
  }

  private static AresDataSchema BuildInputSchema(IEnumerable<ParameterMetadata> parameterMetadatas)
  {
    var schema = new AresDataSchema();
    foreach(var parameterMetadata in parameterMetadatas)
    {
      var entry = parameterMetadata.Schema ?? new SchemaEntry { Type = AresDataType.Any, Optional = true };
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
