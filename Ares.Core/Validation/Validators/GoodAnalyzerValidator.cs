using Ares.Core.Analyzing;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Validators;

public static class GoodAnalyzerValidator
{
  public static async Task<ValidationResult> Validate(ExperimentTemplate experimentTemplate, ExperimentTemplate? startupTemplate, IAnalyzerRepo analyzerRepo)
    => await Validate(experimentTemplate, startupTemplate, analyzerRepo, new Dictionary<string, AresValueSchema>(StringComparer.OrdinalIgnoreCase));

  public static async Task<ValidationResult> Validate(
    ExperimentTemplate experimentTemplate,
    ExperimentTemplate? startupTemplate,
    IAnalyzerRepo analyzerRepo,
    IReadOnlyDictionary<string, AresValueSchema> customCommandOutputSchemas)
  {
    if(!experimentTemplate.HasAnalyzerId || string.IsNullOrWhiteSpace(experimentTemplate.AnalyzerId))
      return new ValidationResult(true);

    var analyzer = analyzerRepo.GetAnalyzerById(experimentTemplate.AnalyzerId);

    if(analyzer is null)
      return new ValidationResult(false, $"Unable to find analyzer with id of {experimentTemplate.AnalyzerId}");

    if(analyzer.AnalyzerState != State.Active)
    {
      return new ValidationResult(false, $"Unable to use analyzer {analyzer.Name} as it is is not currently active.\n{analyzer.StateMessage}");
    }

    var outputCommands = experimentTemplate.GetAllOutputCommands();

    if(startupTemplate is not null)
    {
      outputCommands = outputCommands.Concat(startupTemplate.GetAllOutputCommands()).ToArray();
    }

    var analysisParameterSchema = await analyzer.GetParameters();
    var requiredAnalysisInputs = analysisParameterSchema.Fields.Where(input => !input.Value.Optional).ToArray();
    if(!outputCommands.Any())
    {
      if(!requiredAnalysisInputs.Any())
        return new ValidationResult(true);
      else
        return new ValidationResult(false, $"Experiment does not have any output commands set, but has analyzer {analyzer.Name} assigned");
    }

    var inputSchema = new AresStructSchema();
    var mappingErrors = new List<string>();
    var customSchemas = ToCaseInsensitiveLookup(customCommandOutputSchemas);

    foreach(var map in experimentTemplate.AnalyzerMaps)
    {
      var splitVarName = map.Value.Split('.', StringSplitOptions.RemoveEmptyEntries);
      if(splitVarName.Length == 0)
      {
        mappingErrors.Add($"Analyzer input '{map.Key}' does not reference a command output.");
        continue;
      }

      var matchingCommand = outputCommands.FirstOrDefault(cmd => cmd.HasOutputVarName && cmd.OutputVarName.Equals(splitVarName.First()));

      if(matchingCommand is null)
      {
        mappingErrors.Add($"Analyzer input '{map.Key}' references '{map.Value}', but output variable '{splitVarName[0]}' is unavailable.");
        continue;
      }

      var cmdSchema = ResolveOutputSchema(matchingCommand, customSchemas, out var schemaError);
      if(cmdSchema is null)
      {
        mappingErrors.Add($"Analyzer input '{map.Key}' references '{map.Value}', but {schemaError}");
        continue;
      }

      var matchingSchema = FindNestedSchema(cmdSchema, splitVarName.Skip(1).ToArray());
      if(matchingSchema is null)
      {
        mappingErrors.Add($"Analyzer input '{map.Key}' references '{map.Value}', but that output member is unavailable.");
        continue;
      }
      if(matchingSchema.Type is AresDataType.Unit or AresDataType.UnspecifiedType)
      {
        mappingErrors.Add($"Analyzer input '{map.Key}' references '{map.Value}', but that output member does not have a usable schema.");
        continue;
      }

      inputSchema.Fields[map.Key] = matchingSchema.Clone();
    }

    var result = await analyzer.ValidateInputs(inputSchema);
    var messages = result.Success && mappingErrors.Count > 0
      ? mappingErrors
      : mappingErrors.Concat(result.Messages);
    var validationResult = new ValidationResult(
      result.Success && mappingErrors.Count == 0,
      messages);

    return validationResult;
  }

  private static AresValueSchema? ResolveOutputSchema(
    CommandTemplate command,
    IReadOnlyDictionary<string, AresValueSchema> customCommandOutputSchemas,
    out string error)
  {
    AresValueSchema? schema;
    switch(command.CommandTypeCase)
    {
      case CommandTemplate.CommandTypeOneofCase.DeviceCommand:
        schema = command.DeviceCommand?.Metadata?.OutputMetadata?.DataSchema;
        error = "the device command does not have a usable output schema.";
        break;

      case CommandTemplate.CommandTypeOneofCase.SystemCommand:
        schema = SystemOperationCatalog.Find(command.SystemCommand.Operation)?.OutputSchema;
        error = "the system command does not have a usable output schema.";
        break;

      case CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation:
        var customCommandId = command.CustomCommandInvocation.CustomCommandId;
        schema = customCommandOutputSchemas.TryGetValue(customCommandId, out var customSchema) ? customSchema : null;
        error = $"custom command '{customCommandId}' does not have an available current output schema.";
        break;

      case CommandTemplate.CommandTypeOneofCase.None:
        schema = null;
        error = "the command does not have a command type or usable output schema.";
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(command.CommandTypeCase), command.CommandTypeCase, null);
    }

    return schema?.Type is AresDataType.Unit or AresDataType.UnspecifiedType ? null : schema;
  }

  private static IReadOnlyDictionary<string, AresValueSchema> ToCaseInsensitiveLookup(
    IReadOnlyDictionary<string, AresValueSchema> schemas)
  {
    var lookup = new Dictionary<string, AresValueSchema>(StringComparer.OrdinalIgnoreCase);
    foreach(var schema in schemas)
      lookup.TryAdd(schema.Key, schema.Value);
    return lookup;
  }

  private static AresValueSchema? FindNestedSchema(AresValueSchema schema, string[] keys)
  {
    while(true)
    {
      if(keys.Length == 0)
        return schema;

      if(schema.Type != AresDataType.Struct || schema.StructSchema is null)
        return null;

      var key = keys[0];
      if(!schema.StructSchema.Fields.TryGetValue(key, out var nestedSchema))
        return null;

      schema = nestedSchema;
      keys = keys.Skip(1).ToArray();
    }
  }
}
