using Ares.Core.Analyzing;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Validators;

public static class GoodAnalyzerValidator
{
  public static async Task<ValidationResult> Validate(ExperimentTemplate experimentTemplate, ExperimentTemplate? startupTemplate, IAnalyzerRepo analyzerRepo)
  {
    if(experimentTemplate.AnalyzerId is null)
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

    foreach(var map in experimentTemplate.AnalyzerMaps)
    {
      var splitVarName = map.Value.Split('.');
      if(!splitVarName.Any())
        continue;

      var matchingCommand = outputCommands.FirstOrDefault(cmd => cmd.HasOutputVarName && cmd.OutputVarName.Equals(splitVarName.First()));

      if(matchingCommand is null)
        continue;

      var cmdSchema = matchingCommand.Metadata.OutputMetadata?.DataSchema;
      if(cmdSchema is null)
        continue;

      var matchingSchema = FindNestedSchema(cmdSchema, splitVarName.Skip(1).ToArray());
      if(matchingSchema is not null)
        inputSchema.Fields[map.Key] = matchingSchema.Clone();
    }

    var result = await analyzer.ValidateInputs(inputSchema);
    var validationResult = new ValidationResult(result.Success, result.Messages);

    return validationResult;
  }

  private static AresValueSchema? FindNestedSchema(AresValueSchema schema, string[] keys)
  {
    if(keys.Length == 0)
      return schema;

    if(schema.Type != AresDataType.Struct || schema.StructSchema is null)
      return null;

    var key = keys[0];
    if(!schema.StructSchema.Fields.TryGetValue(key, out var nestedSchema))
      return null;

    return FindNestedSchema(nestedSchema, keys.Skip(1).ToArray());
  }

  public static async Task<ValidationResult> Validate(IEnumerable<ExperimentTemplate> experimentTemplates, ExperimentTemplate startupTemplate, IAnalyzerRepo analyzerManager)
  {
    var validationTasks = experimentTemplates.Select(template => Validate(template, startupTemplate, analyzerManager)).ToArray();
    var validations = await Task.WhenAll(validationTasks);
    return new ValidationResult(validations);
  }
}
