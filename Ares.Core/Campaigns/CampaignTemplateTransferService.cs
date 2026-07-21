using Ares.Core.CustomCommands;
using Ares.Core.Device.Repos;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Core.Campaigns;

internal class CampaignTemplateTransferService(
  ICampaignTemplatePersistenceService campaignPersistenceService,
  ICustomCommandPersistenceService customCommandPersistenceService,
  IAresDeviceRepo deviceRepo,
  IDbContextFactory<CoreDatabaseContext> contextFactory) : ICampaignTemplateTransferService
{
  private readonly JsonSerializerOptions _serializerOptions = SerializerSettingsHelper.CreateCustomSerializationSettings();

  public async Task<CampaignTemplateExport?> ExportAsync(string campaignId, CancellationToken cancellationToken = default)
  {
    var template = await campaignPersistenceService.GetByIdAsync(campaignId, cancellationToken);
    if(template is null)
      return null;

    var json = JsonSerializer.Serialize(template, _serializerOptions);
    return new CampaignTemplateExport(template, json, MakeFileName(template.Name));
  }

  public async Task<CampaignTemplateImportResult> ImportAsync(string json, CancellationToken cancellationToken = default)
  {
    CampaignTemplate template;
    try
    {
      var root = JsonNode.Parse(json) ?? throw new CampaignTemplateImportException("The selected file does not contain a JSON document.");
      CampaignTemplateLegacyJsonConverter.Convert(root);
      template = root.Deserialize<CampaignTemplate>(_serializerOptions)
        ?? throw new CampaignTemplateImportException("The selected file does not contain a campaign template.");
    }
    catch(CampaignTemplateImportException)
    {
      throw;
    }
    catch(Exception exception) when(exception is JsonException or NotSupportedException)
    {
      throw new CampaignTemplateImportException("The selected file is not a valid campaign template JSON file.", exception);
    }

    Validate(template);
    var warnings = new List<string>();
    await PreparePlannerAllocationsAsync(template, warnings, cancellationToken);
    await AddReferenceWarningsAsync(template, warnings);
    await AssignImportIdentityAsync(template, cancellationToken);

    try
    {
      await campaignPersistenceService.AddAsync(template, cancellationToken);
    }
    catch(Exception exception)
    {
      throw new CampaignTemplateImportException("The campaign was valid but could not be saved to the database.", exception);
    }

    return new CampaignTemplateImportResult(template, warnings);
  }

  private static void Validate(CampaignTemplate template)
  {
    if(string.IsNullOrWhiteSpace(template.Name))
      throw new CampaignTemplateImportException("The campaign template does not have a name.");
    if(template.ExperimentTemplate is null)
      throw new CampaignTemplateImportException("The campaign template does not contain a main experiment.");

    var invalidCommand = GetExperiments(template)
      .SelectMany(experiment => experiment.StepTemplates)
      .SelectMany(step => step.CommandTemplates)
      .FirstOrDefault(command => command.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.None);
    if(invalidCommand is not null)
      throw new CampaignTemplateImportException($"Command '{invalidCommand.UniqueId}' does not contain a supported command type.");
  }

  private async Task PreparePlannerAllocationsAsync(CampaignTemplate template, List<string> warnings, CancellationToken cancellationToken)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    var localPlanners = await context.PlannerInfos.AsNoTracking().ToArrayAsync(cancellationToken);
    var parametersById = template.PlannableParameters
      .Where(parameter => !string.IsNullOrWhiteSpace(parameter.UniqueId))
      .ToDictionary(parameter => parameter.UniqueId, StringComparer.OrdinalIgnoreCase);

    foreach(var allocation in template.PlannerAllocations.ToArray())
    {
      var importedPlanner = allocation.Planner;
      var localPlanner = localPlanners.FirstOrDefault(planner => planner.UniqueId == importedPlanner?.UniqueId);
      if(localPlanner is null && importedPlanner is not null)
      {
        var matchingPlanners = localPlanners.Where(planner => planner.Name == importedPlanner.Name
          && planner.Type == importedPlanner.Type
          && planner.Version == importedPlanner.Version).ToArray();
        if(matchingPlanners.Length == 1)
          localPlanner = matchingPlanners[0];
      }
      ParameterMetadata? parameter = null;
      var hasParameter = allocation.Parameter is not null
        && parametersById.TryGetValue(allocation.Parameter.UniqueId, out parameter);
      if(localPlanner is null || !hasParameter)
      {
        template.PlannerAllocations.Remove(allocation);
        warnings.Add(localPlanner is null
          ? $"Planner allocation for '{importedPlanner?.Name ?? "unknown planner"}' was removed because that planner is unavailable."
          : "A planner allocation was removed because its parameter is unavailable.");
        continue;
      }

      allocation.Planner = localPlanner;
      allocation.Parameter = parameter!;
    }
  }

  private async Task AddReferenceWarningsAsync(CampaignTemplate template, List<string> warnings)
  {
    var deviceIds = deviceRepo.GetAll().Select(device => device.UniqueId).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var customCommandIds = (await customCommandPersistenceService.GetCommandsAsync())
      .Select(command => command.CustomCommandId)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach(var command in GetExperiments(template).SelectMany(experiment => experiment.StepTemplates).SelectMany(step => step.CommandTemplates))
      switch(command.CommandTypeCase)
      {
        case CommandTemplate.CommandTypeOneofCase.DeviceCommand:
          var deviceId = command.DeviceCommand?.Metadata?.DeviceId;
          if(!string.IsNullOrWhiteSpace(deviceId) && !deviceIds.Contains(deviceId))
            warnings.Add($"Device '{deviceId}' is unavailable; its command was preserved for repair.");
          break;
        case CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation:
          var customCommandId = command.CustomCommandInvocation.CustomCommandId;
          if(!string.IsNullOrWhiteSpace(customCommandId) && !customCommandIds.Contains(customCommandId))
            warnings.Add($"Custom command '{customCommandId}' is unavailable; its invocation was preserved for repair.");
          break;
        case CommandTemplate.CommandTypeOneofCase.SystemCommand:
        case CommandTemplate.CommandTypeOneofCase.None:
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(command.CommandTypeCase), command.CommandTypeCase, null);
      }
  }

  private async Task AssignImportIdentityAsync(CampaignTemplate template, CancellationToken cancellationToken)
  {
    template.UniqueId = Guid.NewGuid().ToString();
    template.Name = await MakeUniqueNameAsync(template.Name.Trim(), cancellationToken);
    foreach(var parameter in template.PlannableParameters)
      parameter.UniqueId = Guid.NewGuid().ToString();
    foreach(var allocation in template.PlannerAllocations)
      allocation.UniqueId = Guid.NewGuid().ToString();

    foreach(var experiment in GetExperiments(template))
    {
      experiment.UniqueId = Guid.NewGuid().ToString();
      foreach(var step in experiment.StepTemplates)
      {
        step.UniqueId = Guid.NewGuid().ToString();
        foreach(var command in step.CommandTemplates)
        {
          command.UniqueId = Guid.NewGuid().ToString();
          if(command.DeviceCommand?.Metadata is not null)
          {
            command.DeviceCommand.Metadata.UniqueId = Guid.NewGuid().ToString();
            if(command.DeviceCommand.Metadata.OutputMetadata is not null)
              command.DeviceCommand.Metadata.OutputMetadata.UniqueId = Guid.NewGuid().ToString();
            foreach(var metadata in command.DeviceCommand.Metadata.ParameterMetadatas)
              metadata.UniqueId = Guid.NewGuid().ToString();
          }

          foreach(var argument in command.ArgumentBindings)
          {
            argument.UniqueId = Guid.NewGuid().ToString();
            if(argument.Metadata is not null)
              argument.Metadata.UniqueId = Guid.NewGuid().ToString();
            if(argument.PlannedSource?.PlanningMetadata is not null)
              argument.PlannedSource.PlanningMetadata.UniqueId = Guid.NewGuid().ToString();
          }
        }
      }
    }
  }

  private async Task<string> MakeUniqueNameAsync(string name, CancellationToken cancellationToken)
  {
    if(!await campaignPersistenceService.ExistsByNameAsync(name, cancellationToken))
      return name;

    var index = 1;
    while(true)
    {
      var candidate = index == 1 ? $"{name} (Imported)" : $"{name} (Imported {index})";
      if(!await campaignPersistenceService.ExistsByNameAsync(candidate, cancellationToken))
        return candidate;
      index++;
    }
  }

  private static IEnumerable<ExperimentTemplate> GetExperiments(CampaignTemplate template)
  {
    if(template.StartupTemplate is not null)
      yield return template.StartupTemplate;
    if(template.ExperimentTemplate is not null)
      yield return template.ExperimentTemplate;
    if(template.CloseoutTemplate is not null)
      yield return template.CloseoutTemplate;
  }

  private static string MakeFileName(string campaignName)
  {
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitizedName = new string(campaignName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray()).Trim();
    return $"{(string.IsNullOrWhiteSpace(sanitizedName) ? "campaign" : sanitizedName)}.json";
  }
}
