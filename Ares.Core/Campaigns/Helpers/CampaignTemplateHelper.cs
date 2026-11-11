using Ares.Core.Device.Remote;
using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;

namespace Ares.Core.Campaigns.Helpers;

public static class CampaignTemplateHelper
{
  public static CampaignCommandMatch MatchDeviceCommand(CampaignDeviceCommandDependency requirement, IReadOnlyList<DeviceCommandDescriptor> descriptors)
  {
    var descriptor = descriptors.FirstOrDefault(candidate => string.Equals(candidate.Name, requirement.Name, StringComparison.OrdinalIgnoreCase));
    if(descriptor is null)
      return new CampaignCommandMatch { RequiredName = requirement.Name, Status = CampaignCommandMatchStatus.Missing, Description = "Command is unavailable." };

    var inputMatches = requirement.Parameters.Count == (descriptor.InputSchema?.Fields.Count ?? 0)
      && requirement.Parameters.All(parameter => descriptor.InputSchema?.Fields.TryGetValue(parameter.Name, out var schema) == true && (parameter.Schema is null || parameter.Schema.Equals(schema)));
    var outputMatches = (requirement.OutputSchema is null && descriptor.OutputSchema is null)
      || (requirement.OutputSchema is not null && requirement.OutputSchema.Equals(descriptor.OutputSchema));
    var status = inputMatches && outputMatches ? CampaignCommandMatchStatus.Exact : CampaignCommandMatchStatus.SchemaMismatch;
    return new CampaignCommandMatch
    {
      RequiredName = requirement.Name,
      CurrentName = descriptor.Name,
      Status = status,
      Description = status == CampaignCommandMatchStatus.Exact ? "Command and interface match." : "Command name matches but its interface differs."
    };
  }

  public static DeviceCategory GetDeviceCategory(IAresDevice device)
  {
    return device switch
    {
      RemoteDevice => DeviceCategory.Remote,
      SilaDevice => DeviceCategory.Sila,
      _ => DeviceCategory.NativePlugin
    };
  }

  public static bool IsCustomCommandSignatureCompatible(CustomCommandRequirement requirement, CustomCommandVersion command)
  {
    if(requirement.InputParameters.Count == 0 && requirement.OutputSchema is null && string.IsNullOrWhiteSpace(requirement.Name))
      return true;

    return CreateCustomCommandCandidate(requirement, command).Status == CampaignCommandMatchStatus.Compatible;
  }

  public static CampaignCustomCommandCandidate CreateCustomCommandCandidate(CustomCommandRequirement requirement, CustomCommandVersion command)
  {
    var nameMatch = NormalizeName(requirement.Name) == NormalizeName(command.Name);
    var inputMatch = requirement.InputParameters.Count == command.InputParameters.Count
      && requirement.InputParameters.All(required => command.InputParameters.Any(input => string.Equals(input.Name, required.Name, StringComparison.OrdinalIgnoreCase)
        && (required.Schema is null || required.Schema.Equals(input.Schema))));
    var outputMatch = (requirement.OutputSchema is null && command.OutputSchema is null)
      || (requirement.OutputSchema is not null && requirement.OutputSchema.Equals(command.OutputSchema));
    var status = inputMatch && outputMatch
      ? CampaignCommandMatchStatus.Compatible
      : inputMatch || outputMatch
        ? CampaignCommandMatchStatus.SchemaMismatch
        : CampaignCommandMatchStatus.Missing;
    var score = (int)(nameMatch ? 50 : NameSimilarity(requirement.Name, command.Name) * 30)
      + (inputMatch ? 40 : 0)
      + (outputMatch ? 30 : 0);
    return new CampaignCustomCommandCandidate
    {
      CustomCommandId = command.CustomCommandId,
      Name = command.Name,
      Description = command.Description,
      Status = status,
      Score = score,
      MatchDescription = $"Name {(nameMatch ? "matches" : "is similar")}; input interface {(inputMatch ? "matches" : "differs")}; output interface {(outputMatch ? "matches" : "differs")}."
    };
  }

  public static double NameSimilarity(string? left, string? right)
  {
    var leftTokens = NormalizeName(left).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    var rightTokens = NormalizeName(right).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    if(leftTokens.Count == 0 || rightTokens.Count == 0)
      return 0;
    return leftTokens.Intersect(rightTokens).Count() / (double)leftTokens.Union(rightTokens).Count();
  }

  public static string NormalizeName(string? name)
    => string.Join(' ', (name ?? string.Empty).Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

  public static void ApplySelections(CampaignImportAnalysis analysis)
  {
    foreach(var dependency in analysis.DeviceDependencies.Where(dependency => !string.IsNullOrWhiteSpace(dependency.SelectedReplacementDeviceId)))
      foreach(var command in GetExperiments(analysis.Template).SelectMany(experiment => experiment.StepTemplates).SelectMany(step => step.CommandTemplates))
        if(command.DeviceCommand?.Metadata is { } metadata && string.Equals(metadata.DeviceId, dependency.DeviceId, StringComparison.OrdinalIgnoreCase))
          metadata.DeviceId = dependency.SelectedReplacementDeviceId!;

    foreach(var dependency in analysis.CustomCommandDependencies.Where(dependency => !string.IsNullOrWhiteSpace(dependency.SelectedReplacementCustomCommandId)))
      foreach(var command in GetExperiments(analysis.Template).SelectMany(experiment => experiment.StepTemplates).SelectMany(step => step.CommandTemplates))
        if(command.CustomCommandInvocation is { } invocation && string.Equals(invocation.CustomCommandId, dependency.CustomCommandId, StringComparison.OrdinalIgnoreCase))
          invocation.CustomCommandId = dependency.SelectedReplacementCustomCommandId!;
  }

  public static CustomCommandRequirement BuildCustomCommandRequirement(CommandTemplate command)
  {
    var invocation = command.CustomCommandInvocation;
    if(invocation?.Requirement is not null)
      return invocation.Requirement.Clone();

    var requirement = new CustomCommandRequirement { Name = string.Empty };
    foreach(var binding in command.ArgumentBindings.Where(binding => binding.Metadata is not null))
      requirement.InputParameters.Add(new CustomCommandParameterRequirement
      {
        Name = binding.Metadata.Name,
        Index = checked((int)binding.Metadata.Index),
        Schema = binding.Metadata.Schema?.Clone() ?? new AresValueSchema()
      });
    return requirement;
  }

  public static IEnumerable<ExperimentTemplate> GetExperiments(CampaignTemplate template)
  {
    if(template.StartupTemplate is not null)
      yield return template.StartupTemplate;
    if(template.ExperimentTemplate is not null)
      yield return template.ExperimentTemplate;
    if(template.CloseoutTemplate is not null)
      yield return template.CloseoutTemplate;
  }

  public static string MakeFileName(string campaignName)
  {
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitizedName = new string(campaignName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray()).Trim();
    return $"{(string.IsNullOrWhiteSpace(sanitizedName) ? "campaign" : sanitizedName)}.json";
  }

  public static void Validate(CampaignTemplate template)
  {
    if(string.IsNullOrWhiteSpace(template.Name))
      throw new CampaignTemplateImportException("The campaign template does not have a name.");
    if(template.ExperimentTemplate is null)
      throw new CampaignTemplateImportException("The campaign template does not contain a main experiment.");

    var invalidCommand = CampaignTemplateHelper.GetExperiments(template)
      .SelectMany(experiment => experiment.StepTemplates)
      .SelectMany(step => step.CommandTemplates)
      .FirstOrDefault(command => command.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.None);
    if(invalidCommand is not null)
      throw new CampaignTemplateImportException($"Command '{invalidCommand.UniqueId}' does not contain a supported command type.");
  }

  public static async Task<IReadOnlyList<CampaignCommandMatch>> MatchCurrentDeviceCommandsAsync(
  IAresDevice device,
  IReadOnlyList<CampaignDeviceCommandDependency> requirements,
  CancellationToken cancellationToken)
  {
    try
    {
      var descriptors = await device.GetCommandDescriptorsAsync();
      cancellationToken.ThrowIfCancellationRequested();
      return requirements.Select(requirement => CampaignTemplateHelper.MatchDeviceCommand(requirement, descriptors)).ToArray();
    }
    catch
    {
      return requirements.Select(requirement => new CampaignCommandMatch
      {
        RequiredName = requirement.Name,
        Status = CampaignCommandMatchStatus.Missing,
        Description = "The current device command interface could not be loaded."
      }).ToArray();
    }
  }
}