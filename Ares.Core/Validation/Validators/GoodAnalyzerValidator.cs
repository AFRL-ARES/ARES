using Ares.Core.Analyzing;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
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

    //TODO: This should be more robust. Even slightly improved like this we're still not going to process nested structs very well, but that's an unlikely scenario for now.
    foreach(var map in experimentTemplate.AnalyzerMaps)
    {
      var matchingCommand = outputCommands.FirstOrDefault(cmd => cmd.UserOutputKeyMap.Values.Contains(map.Value));

      if(matchingCommand is null)
        continue;

      var matchingMap = matchingCommand.UserOutputKeyMap.FirstOrDefault(userMap => userMap.Value == map.Value);
      //var outputAresValueSchema = matchingCommand.Metadata.OutputMetadata.DataSchema;
      var matchingOutputSchema = matchingCommand.Metadata.OutputMetadata;

      if(matchingOutputSchema.DataSchema.Type == AresDataType.Struct)
      {
        //Look for matching values internal to the struct
        var matched = matchingOutputSchema.DataSchema.StructSchema.Fields.TryGetValue(map.Value, out var matchingValue);
        
        if(matched)
          inputSchema.AddEntry(map.Key, matchingValue.Type);

        //If we don't find a match inside the struct assume the type requested was a struct
        else
          inputSchema.AddEntry(map.Key, AresDataType.Struct);
      }

      else
        inputSchema.AddEntry(map.Key, matchingOutputSchema.DataSchema.Type);
    }

    var result = await analyzer.ValidateInputs(inputSchema);
    var validationResult = new ValidationResult(result.Success, result.Messages);

    return validationResult;
  }

  public static async Task<ValidationResult> Validate(IEnumerable<ExperimentTemplate> experimentTemplates, ExperimentTemplate startupTemplate, IAnalyzerRepo analyzerManager)
  {
    var validationTasks = experimentTemplates.Select(template => Validate(template, startupTemplate, analyzerManager)).ToArray();
    var validations = await Task.WhenAll(validationTasks);
    return new ValidationResult(validations);
  }
}
