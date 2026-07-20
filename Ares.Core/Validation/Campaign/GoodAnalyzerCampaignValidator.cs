using Ares.Core.Analyzing;
using Ares.Core.CustomCommands;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Core.Validation.Validators;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Campaign;

public class GoodAnalyzerCampaignValidator : ICampaignValidator
{
  private readonly IAnalyzerRepo _analyzerManager;
  private readonly ICustomCommandPersistenceService _customCommandPersistenceService;

  public GoodAnalyzerCampaignValidator(
    IAnalyzerRepo analyzerManager,
    ICustomCommandPersistenceService customCommandPersistenceService)
  {
    _analyzerManager = analyzerManager;
    _customCommandPersistenceService = customCommandPersistenceService;
  }

  public async Task<ValidationResult> Validate(CampaignTemplate template)
  {
    if(!template.ExperimentTemplate.HasAnalyzerId || string.IsNullOrWhiteSpace(template.ExperimentTemplate.AnalyzerId))
      return await GoodAnalyzerValidator.Validate(template.ExperimentTemplate, template.StartupTemplate, _analyzerManager);

    var outputCommands = template.ExperimentTemplate.GetAllOutputCommands();
    if(template.StartupTemplate is not null)
      outputCommands = outputCommands.Concat(template.StartupTemplate.GetAllOutputCommands()).ToArray();

    var hasCustomCommandOutput = outputCommands.Any(command =>
      command.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation);
    if(!hasCustomCommandOutput)
      return await GoodAnalyzerValidator.Validate(template.ExperimentTemplate, template.StartupTemplate, _analyzerManager);

    IReadOnlyDictionary<string, AresValueSchema> customCommandOutputSchemas;
    try
    {
      customCommandOutputSchemas = (await _customCommandPersistenceService.GetCommandsAsync())
        .Where(command => !string.IsNullOrWhiteSpace(command.CustomCommandId) && command.OutputSchema is not null)
        .GroupBy(command => command.CustomCommandId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().OutputSchema!, StringComparer.OrdinalIgnoreCase);
    }
    catch(Exception exception)
    {
      return new ValidationResult(false, $"Unable to load current custom-command definitions for analyzer validation. {exception.Message}");
    }

    return await GoodAnalyzerValidator.Validate(
      template.ExperimentTemplate,
      template.StartupTemplate,
      _analyzerManager,
      customCommandOutputSchemas);
  }
}
