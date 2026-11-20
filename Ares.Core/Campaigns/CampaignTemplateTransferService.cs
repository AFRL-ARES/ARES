using Ares.Core.CustomCommands;
using Ares.Core.Device.Providers;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Core.Campaigns.Helpers;
using Ares.Core.Campaigns.Models;

namespace Ares.Core.Campaigns;

internal class CampaignTemplateTransferService(
  ICampaignTemplatePersistenceService campaignPersistenceService,
  ICustomCommandPersistenceService customCommandPersistenceService,
  IAresDeviceProvider deviceProvider,
  IDeviceConfigProvider deviceConfigProvider,
  IDbContextFactory<CoreDatabaseContext> contextFactory) : ICampaignTemplateTransferService
{
  private readonly JsonSerializerOptions _serializerOptions = SerializerSettingsHelper.CreateCustomSerializationSettings();

  public async Task<CampaignTemplateExport?> ExportAsync(string campaignId, CancellationToken cancellationToken = default)
  {
    var template = await campaignPersistenceService.GetByIdAsync(campaignId, cancellationToken);
    if(template is null)
      return null;

    var json = JsonSerializer.Serialize(template, _serializerOptions);
    return new CampaignTemplateExport(template, json, CampaignTemplateHelper.MakeFileName(template.Name));
  }

  public async Task<CampaignImportAnalysis> AnalyzeImportAsync(string json, CancellationToken cancellationToken = default)
  {
    var template = ParseTemplate(json);
    CampaignTemplateHelper.Validate(template);
    var warnings = new List<string>();
    await PreparePlannerAllocationsAsync(template, warnings, cancellationToken);
    var dependencies = await AnalyzeDependenciesAsync(template, warnings, cancellationToken);
    return new CampaignImportAnalysis
    {
      Template = template,
      Warnings = warnings,
      DeviceDependencies = dependencies.Devices,
      CustomCommandDependencies = dependencies.CustomCommands
    };
  }

  public Task<CampaignTemplateImportResult> ImportAsync(string json, CancellationToken cancellationToken = default)
    => AnalyzeAndCommitAsync(json, cancellationToken);

  public async Task<CampaignTemplateImportResult> CommitImportAsync(CampaignImportAnalysis analysis, CancellationToken cancellationToken = default)
  {
    CampaignTemplateHelper.ApplySelections(analysis);
    await AssignImportIdentityAsync(analysis.Template, cancellationToken);

    try
    {
      await campaignPersistenceService.AddAsync(analysis.Template, cancellationToken);
    }
    catch(Exception exception)
    {
      throw new CampaignTemplateImportException("The campaign was valid but could not be saved to the database.", exception);
    }

    return new CampaignTemplateImportResult(analysis.Template, analysis.Warnings);
  }

  private async Task<CampaignTemplateImportResult> AnalyzeAndCommitAsync(string json, CancellationToken cancellationToken)
  {
    var analysis = await AnalyzeImportAsync(json, cancellationToken);
    return await CommitImportAsync(analysis, cancellationToken);
  }

  private CampaignTemplate ParseTemplate(string json)
  {
    try
    {
      var root = JsonNode.Parse(json) ?? throw new CampaignTemplateImportException("The selected file does not contain a JSON document.");
      CampaignTemplateLegacyJsonConverter.Convert(root);
      return root.Deserialize<CampaignTemplate>(_serializerOptions)
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

  private async Task<(IReadOnlyList<CampaignDeviceDependency> Devices, IReadOnlyList<CampaignCustomCommandDependency> CustomCommands)> AnalyzeDependenciesAsync(
    CampaignTemplate template,
    List<string> warnings,
    CancellationToken cancellationToken)
  {
    var commands = CampaignTemplateHelper.GetExperimentTemplates(template)
      .SelectMany(experiment => experiment.StepTemplates
      .SelectMany(step => step.CommandTemplates
      .Select(command => (ExperimentName: experiment.Name, StepId: step.UniqueId, command))))
      .ToArray();

    var devicesById = commands
      .Where(item => item.command.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.DeviceCommand)
      .Select(item => (item.command.DeviceCommand?.Metadata, item.ExperimentName, item.StepId, item.command.UniqueId))
      .Where(item => item.Metadata is not null && !string.IsNullOrWhiteSpace(item.Metadata.DeviceId))
      .GroupBy(item => item.Metadata!.DeviceId, StringComparer.OrdinalIgnoreCase);

    var devices = new List<CampaignDeviceDependency>();

    foreach(var deviceGroup in devicesById)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var firstMetadata = deviceGroup.First().Metadata!;
      var currentDevice = deviceProvider.GetDevice(deviceGroup.Key);
      var commandDependencies = deviceGroup
        .GroupBy(item => item.Metadata!.Name, StringComparer.OrdinalIgnoreCase)
        .Select(commandGroup => new CampaignDeviceCommandDependency
        {
          CommandId = commandGroup.First().Metadata!.UniqueId,
          Name = commandGroup.Key,
          Parameters = commandGroup.First().Metadata!.ParameterMetadatas.Select(metadata => metadata.Clone()).ToArray(),
          OutputSchema = commandGroup.First().Metadata!.OutputMetadata?.DataSchema?.Clone(),
          Locations = commandGroup.Select(item => $"{item.ExperimentName} / {item.StepId} / {item.Metadata!.Name}").Distinct().ToArray()
        })
        .ToArray();

      if(currentDevice is not null)
      {
        var currentMatches = await CampaignTemplateHelper.MatchCurrentDeviceCommandsAsync(currentDevice, commandDependencies, cancellationToken);
        if(currentMatches.Any(match => match.Status != CampaignCommandMatchStatus.Exact))
          warnings.Add($"Device '{firstMetadata.DeviceType}' is available, but one or more commands no longer match its current command interface.");

        devices.Add(new CampaignDeviceDependency
        {
          DeviceId = deviceGroup.Key,
          DeviceType = firstMetadata.DeviceType,
          DeviceCategory = firstMetadata.HasDeviceCategory ? firstMetadata.DeviceCategory : DeviceCategory.Unspecified,
          DriverId = firstMetadata.DeviceDriverId,
          Commands = commandDependencies,
          Candidates = [],
          CurrentCommandMatches = currentMatches,
          CommandCandidates = await FindCommandCandidatesAsync(currentDevice, commandDependencies, cancellationToken),
          IsResolved = true
        });
        continue;
      }

      warnings.Add($"Device '{firstMetadata.DeviceType}' ({deviceGroup.Key}) is unavailable; its command was preserved for repair.");
      var candidates = await FindDeviceCandidatesAsync(firstMetadata, commandDependencies, cancellationToken);
      devices.Add(new CampaignDeviceDependency
      {
        DeviceId = deviceGroup.Key,
        DeviceType = firstMetadata.DeviceType,
        DeviceCategory = firstMetadata.HasDeviceCategory ? firstMetadata.DeviceCategory : DeviceCategory.Unspecified,
        DriverId = firstMetadata.DeviceDriverId,
        Commands = commandDependencies,
        Candidates = candidates,
        CurrentCommandMatches = [],
        IsResolved = false
      });
    }

    var customCommands = await customCommandPersistenceService.GetCommandsAsync();
    var customCommandsById = customCommands
      .Where(command => !string.IsNullOrWhiteSpace(command.CustomCommandId))
      .ToDictionary(command => command.CustomCommandId, StringComparer.OrdinalIgnoreCase);

    var customDependencies = commands
      .Where(item => item.command.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation)
      .GroupBy(item => item.command.CustomCommandInvocation.CustomCommandId, StringComparer.OrdinalIgnoreCase)
      .Where(group => !string.IsNullOrWhiteSpace(group.Key))
      .Select(group => (Group: group, Requirement: CampaignTemplateHelper.BuildCustomCommandRequirement(group.First().command)))
      .ToArray();

    var customResults = new List<CampaignCustomCommandDependency>();

    foreach(var (Group, Requirement) in customDependencies)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var isResolved = customCommandsById.TryGetValue(Group.Key, out var currentCommand);
      var currentSignatureMatches = !isResolved || CampaignTemplateHelper.IsCustomCommandSignatureCompatible(Requirement, currentCommand!);

      if(!isResolved)
        warnings.Add($"Custom command '{Group.Key}' is unavailable; its invocation was preserved for repair.");
      
      else if(!currentSignatureMatches)
        warnings.Add($"Custom command '{Group.Key}' is available, but its current interface differs from the campaign requirement.");

      var candidates = customCommands
        .Where(command => !string.Equals(command.CustomCommandId, Group.Key, StringComparison.OrdinalIgnoreCase))
        .Select(command => CampaignTemplateHelper.CreateCustomCommandCandidate(Requirement, command))
        .OrderByDescending(candidate => candidate.Score)
        .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

      customResults.Add(new CampaignCustomCommandDependency
      {
        CustomCommandId = Group.Key,
        Requirement = Requirement,
        Locations = Group.Select(item => $"{item.ExperimentName} / {item.StepId} / Custom Command").Distinct().ToArray(),
        Candidates = candidates,
        IsResolved = isResolved,
        CurrentSignatureMatches = currentSignatureMatches
      });
    }

    return (devices, customResults);
  }

  private static async Task<IReadOnlyDictionary<string, IReadOnlyList<CampaignDeviceCommandCandidate>>> FindCommandCandidatesAsync(
    IAresDevice device,
    IReadOnlyList<CampaignDeviceCommandDependency> requirements,
    CancellationToken cancellationToken)
  {
    IReadOnlyList<DeviceCommandDescriptor> descriptors;
    try
    {
      descriptors = await device.GetCommandDescriptorsAsync();
      cancellationToken.ThrowIfCancellationRequested();
    }
    catch
    {
      descriptors = [];
    }

    return requirements.ToDictionary(
      requirement => requirement.CommandId,
      requirement => (IReadOnlyList<CampaignDeviceCommandCandidate>)CampaignTemplateHelper.GetDeviceCommandCandidates(requirement, descriptors),
      StringComparer.OrdinalIgnoreCase);
  }

  private async Task<IReadOnlyList<CampaignDeviceCandidate>> FindDeviceCandidatesAsync(
    CommandMetadata metadata, 
    IReadOnlyList<CampaignDeviceCommandDependency> requirements, 
    CancellationToken cancellationToken)
  {
    var candidates = new List<CampaignDeviceCandidate>();
    foreach(var device in deviceProvider.GetAllDevices())
    {
      cancellationToken.ThrowIfCancellationRequested();
      var category = CampaignTemplateHelper.GetDeviceCategory(device);
      if(metadata.HasDeviceCategory && metadata.DeviceCategory != DeviceCategory.Unspecified && category != metadata.DeviceCategory)
        continue;

      IReadOnlyList<DeviceCommandDescriptor> descriptors;
      try
      {
        descriptors = await device.GetCommandDescriptorsAsync();
      }
      catch
      {
        descriptors = [];
      }

      var matches = requirements.Select(requirement => CampaignTemplateHelper.MatchDeviceCommand(requirement, descriptors)).ToArray();
      var commandCandidates = requirements.ToDictionary(
        requirement => requirement.CommandId,
        requirement => CampaignTemplateHelper.GetDeviceCommandCandidates(requirement, descriptors),
        StringComparer.OrdinalIgnoreCase);
      var score = matches.Sum(match => match.Status switch
      {
        CampaignCommandMatchStatus.Exact => 50,
        CampaignCommandMatchStatus.Compatible => 35,
        CampaignCommandMatchStatus.SchemaMismatch => 5,
        CampaignCommandMatchStatus.Missing => 0,
        _ => 0
      }) + matches.Sum(match => (int)(CampaignTemplateHelper.NameSimilarity(match.RequiredName, match.CurrentName) * 30));
      if(metadata.HasDeviceDriverId && !string.IsNullOrWhiteSpace(metadata.DeviceDriverId)
        && string.Equals(metadata.DeviceDriverId, GetDeviceDriverId(device), StringComparison.OrdinalIgnoreCase))
        score += 100;
      if(!string.IsNullOrWhiteSpace(metadata.DeviceType) && string.Equals(metadata.DeviceType, device.Type, StringComparison.OrdinalIgnoreCase))
        score += 25;

      candidates.Add(new CampaignDeviceCandidate
        {
          DeviceId = device.UniqueId,
          Name = device.Name,
          Type = device.Type,
          Category = category,
          DriverId = GetDeviceDriverId(device),
          Score = score,
          CommandMatches = matches,
          CommandCandidates = commandCandidates
        });
    }

    var orderedCandidates = candidates.OrderByDescending(candidate => candidate.Score)
      .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase);
    return metadata.HasDeviceCategory && metadata.DeviceCategory == DeviceCategory.Remote
      ? orderedCandidates.ToArray()
      : orderedCandidates.Take(10).ToArray();
  }

  private string GetDeviceDriverId(IAresDevice device)
    => deviceConfigProvider.GetConfigByDeviceId(device.UniqueId)?.DriverId ?? string.Empty;

  private async Task AssignImportIdentityAsync(CampaignTemplate template, CancellationToken cancellationToken)
  {
    template.UniqueId = Guid.NewGuid().ToString();
    template.Name = await MakeUniqueNameAsync(template.Name.Trim(), cancellationToken);
    
    var importedPlannableParameterIds = template.PlannableParameters
      .Where(parameter => !string.IsNullOrWhiteSpace(parameter.UniqueId))
      .ToDictionary(parameter => parameter.UniqueId, StringComparer.OrdinalIgnoreCase);
    var plannableParameterIdMap = importedPlannableParameterIds.Keys
      .ToDictionary(id => id, _ => Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase);
    foreach(var parameter in template.PlannableParameters)
      parameter.UniqueId = plannableParameterIdMap.TryGetValue(parameter.UniqueId, out var importedId)
        ? importedId
        : Guid.NewGuid().ToString();
    
    foreach(var allocation in template.PlannerAllocations)
      allocation.UniqueId = Guid.NewGuid().ToString();

    foreach(var experiment in CampaignTemplateHelper.GetExperimentTemplates(template))
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
            command.DeviceCommand.Metadata.OutputMetadata?.UniqueId = Guid.NewGuid().ToString();

            foreach(var metadata in command.DeviceCommand.Metadata.ParameterMetadatas)
              metadata.UniqueId = Guid.NewGuid().ToString();
          }

          foreach(var argument in command.ArgumentBindings)
          {
            argument.UniqueId = Guid.NewGuid().ToString();
            argument.Metadata?.UniqueId = Guid.NewGuid().ToString();

            if(argument.PlannedSource?.PlanningMetadata is { } planningMetadata)
              planningMetadata.UniqueId = plannableParameterIdMap.TryGetValue(planningMetadata.UniqueId, out var importedId)
                ? importedId
                : Guid.NewGuid().ToString();
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
}