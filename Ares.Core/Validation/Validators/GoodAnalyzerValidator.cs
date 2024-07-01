using Ares.Core.Analyzing;
using Ares.Messaging;

namespace Ares.Core.Validation.Validators;

public static class GoodAnalyzerValidator
{
  public static ValidationResult Validate(CommandTemplate outputCommand, IAnalyzer analyzer)
  {
    var outputMetadata = outputCommand.Metadata.OutputMetadata;
    if (outputMetadata is null)
      return new ValidationResult(false, $"Output command {outputCommand.Metadata.DeviceName}:{outputCommand.Metadata.Name} does not have output metadata defined");

    return Validate(outputMetadata, analyzer);
  }

  public static ValidationResult Validate(OutputMetadata outputMetadata, IAnalyzer analyzer)
  {
    var outputType = outputMetadata.FullName;
    var supported = analyzer.InputSupported(outputType);

    return new ValidationResult(supported, supported ? string.Empty : $"Analyzer {analyzer.Name}:{analyzer.Version} of type {analyzer.GetType().FullName} does not support analyzing of {outputType}");
  }

  public static ValidationResult Validate(ExperimentTemplate experimentTemplate, IAnalyzerManager analyzerManager)
  {
    if (experimentTemplate.Analyzer is null)
      return new ValidationResult(true);

    var outputCommand = experimentTemplate.StepTemplates.SelectMany(template => template.CommandTemplates).FirstOrDefault(template => template.UniqueId == experimentTemplate.OutputCommandId);
    if (outputCommand is null)
      if (experimentTemplate.Analyzer is null)
        return new ValidationResult(true);
      else
        return new ValidationResult(false, $"Experiment does not have an output command set, but has analyzer {experimentTemplate.Analyzer} assigned");

    var analyzer = analyzerManager.GetAnalyzer(experimentTemplate.Analyzer);
    if (analyzer is null)
      return new ValidationResult(false, $"Unable to find analyzer {experimentTemplate.Analyzer}");

    return Validate(outputCommand, analyzer);
  }

  public static ValidationResult Validate(IEnumerable<ExperimentTemplate> experimentTemplates, IAnalyzerManager analyzerManager)
  {
    var validations = experimentTemplates.Select(template => Validate(template, analyzerManager)).ToArray();
    return new ValidationResult(validations.All(result => result.Success), validations.SelectMany(result => result.Messages));
  }
}
