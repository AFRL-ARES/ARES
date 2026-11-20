using Ares.Core.Campaigns.Models;
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
    var match = GetDeviceCommandMatchCandidates(requirement, descriptors).FirstOrDefault();
    return match is null
      ? new CampaignCommandMatch
      {
        RequiredName = requirement.Name,
        Status = CampaignCommandMatchStatus.Missing,
        Description = "No matching command interface found."
      }
      : CreateDeviceCommandMatch(requirement, match.Descriptor, match.IsNameMatch);
  }

  public static IReadOnlyList<CampaignDeviceCommandCandidate> GetDeviceCommandCandidates(
    CampaignDeviceCommandDependency requirement,
    IReadOnlyList<DeviceCommandDescriptor> descriptors)
    => GetDeviceCommandMatchCandidates(requirement, descriptors)
      .Select(candidate => new CampaignDeviceCommandCandidate
      {
        RequiredName = requirement.Name,
        Name = candidate.Descriptor.Name,
        Status = candidate.Status,
        Score = candidate.Score,
        MatchDescription = candidate.MatchDescription
      })
      .ToArray();

  private static IEnumerable<DeviceCommandMatchCandidate> GetDeviceCommandMatchCandidates(
    CampaignDeviceCommandDependency requirement,
    IReadOnlyList<DeviceCommandDescriptor> descriptors)
    => descriptors
      .Select(descriptor =>
      {
        var isNameMatch = string.Equals(descriptor.Name, requirement.Name, StringComparison.OrdinalIgnoreCase);
        var interfaceMatches = DeviceCommandInterfaceMatches(requirement, descriptor);
        var nameSimilarity = NameSimilarity(requirement.Name, descriptor.Name);
        var status = isNameMatch && interfaceMatches
          ? CampaignCommandMatchStatus.Exact
          : interfaceMatches
            ? CampaignCommandMatchStatus.Compatible
            : nameSimilarity > 0
              ? CampaignCommandMatchStatus.SchemaMismatch
              : CampaignCommandMatchStatus.Missing;
        var score = status switch
        {
          CampaignCommandMatchStatus.Exact => 50,
          CampaignCommandMatchStatus.Compatible => 35,
          CampaignCommandMatchStatus.SchemaMismatch => 5,
          CampaignCommandMatchStatus.Missing => 0,
          _ => 0
        } + (int)(nameSimilarity * 30);
        return new DeviceCommandMatchCandidate(
          descriptor,
          isNameMatch,
          status,
          score,
          GetDeviceCommandMatchDescription(descriptor, status));
      })
      .Where(candidate => candidate.Status is not CampaignCommandMatchStatus.Missing)
      .OrderByDescending(candidate => candidate.Score)
      .ThenBy(candidate => candidate.Descriptor.Name, StringComparer.OrdinalIgnoreCase);

  private sealed record DeviceCommandMatchCandidate(
    DeviceCommandDescriptor Descriptor,
    bool IsNameMatch,
    CampaignCommandMatchStatus Status,
    int Score,
    string MatchDescription);

  private static string GetDeviceCommandMatchDescription(DeviceCommandDescriptor descriptor, CampaignCommandMatchStatus status)
    => status switch
    {
      CampaignCommandMatchStatus.Exact => "Command and interface match.",
      CampaignCommandMatchStatus.Compatible => $"Command name differs ({descriptor.Name}), but the interface matches.",
      CampaignCommandMatchStatus.SchemaMismatch => $"Command name is similar ({descriptor.Name}), but its interface differs.",
      CampaignCommandMatchStatus.Missing => $"No interface match; command is {descriptor.Name}.",
      _ => "Command match status is unknown."
    };

  private static CampaignCommandMatch CreateDeviceCommandMatch(
    CampaignDeviceCommandDependency requirement,
    DeviceCommandDescriptor descriptor,
    bool isNameMatch)
  {
    var interfaceMatches = DeviceCommandInterfaceMatches(requirement, descriptor);
    CampaignCommandMatchStatus status;
    string description;
    if(isNameMatch && interfaceMatches)
    {
      status = CampaignCommandMatchStatus.Exact;
      description = "Command and interface match.";
    }
    else if(interfaceMatches)
    {
      status = CampaignCommandMatchStatus.Compatible;
      description = $"Command name differs ({descriptor.Name}), but the interface matches.";
    }
    else
    {
      status = CampaignCommandMatchStatus.SchemaMismatch;
      description = $"Command name is similar ({descriptor.Name}), but its interface differs.";
    }

    return new CampaignCommandMatch
    {
      RequiredName = requirement.Name,
      CurrentName = descriptor.Name,
      Status = status,
      Description = description
    };
  }

  private static bool DeviceCommandInterfaceMatches(CampaignDeviceCommandDependency requirement, DeviceCommandDescriptor descriptor)
  {
    var inputMatches = requirement.Parameters.Count == (descriptor.InputSchema?.Fields.Count ?? 0)
      && requirement.Parameters.All(parameter => descriptor.InputSchema?.Fields.TryGetValue(parameter.Name, out var schema) == true && (parameter.Schema is null || parameter.Schema.Equals(schema)));
    var outputMatches = (requirement.OutputSchema is null && descriptor.OutputSchema is null)
      || (requirement.OutputSchema is not null && requirement.OutputSchema.Equals(descriptor.OutputSchema));
    return inputMatches && outputMatches;
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
    bool hasName = !string.IsNullOrWhiteSpace(requirement.Name);
    bool nameMatches = hasName && NormalizeName(requirement.Name) == NormalizeName(command.Name);

    int matchingInputNameCount = CountMatchingInputNames(requirement.InputParameters.ToList(), command.InputParameters.ToList());
    int matchingInputCount = CountMatchingInputSchemas(requirement.InputParameters.ToList(), command.InputParameters.ToList());

    bool inputMatch = requirement.InputParameters.Count == command.InputParameters.Count && matchingInputCount == requirement.InputParameters.Count;

    bool outputMatch = AreOutputSchemasEqual(requirement.OutputSchema, command.OutputSchema);

    var status = DetermineMatchStatus(inputMatch, outputMatch, matchingInputNameCount);
    int score = CalculateScore(requirement, command, hasName, nameMatches, matchingInputNameCount, matchingInputCount, outputMatch);

    return new CampaignCustomCommandCandidate
    {
      CustomCommandId = command.CustomCommandId,
      Name = command.Name,
      Description = command.Description,
      Status = status,
      Score = score,
      MatchDescription = BuildMatchDescription(requirement, command, nameMatches, inputMatch, matchingInputNameCount, matchingInputCount, outputMatch)
    };
  }

  private static int CountMatchingInputNames(List<CustomCommandParameterRequirement> requirements, List<CustomCommandParameter> inputs)
    => requirements.Count(req => inputs.Any(inp => string.Equals(inp.Name, req.Name, StringComparison.OrdinalIgnoreCase)));

  private static int CountMatchingInputSchemas(List<CustomCommandParameterRequirement> requirements, List<CustomCommandParameter> inputs)
  {
    return requirements.Count(req => inputs.Any(inp => string.Equals(inp.Name, req.Name, StringComparison.OrdinalIgnoreCase)
                          && (req.Schema is null || req.Schema.Equals(inp.Schema))));
  }

  private static bool AreOutputSchemasEqual(AresValueSchema? reqSchema, AresValueSchema? comSchema)
    => (reqSchema is null && comSchema is null) || (reqSchema is not null && reqSchema.Equals(comSchema));

  private static CampaignCommandMatchStatus DetermineMatchStatus(bool inputMatch, bool outputMatch, int matchingInputNameCount)
  {
    if(inputMatch && outputMatch)
      return CampaignCommandMatchStatus.Compatible;

    if(inputMatch || outputMatch || matchingInputNameCount > 0)
      return CampaignCommandMatchStatus.SchemaMismatch;

    return CampaignCommandMatchStatus.Missing;
  }

  private static int CalculateScore(
      CustomCommandRequirement requirement,
      CustomCommandVersion command,
      bool hasName,
      bool nameMatches,
      int matchingInputNameCount,
      int matchingInputCount,
      bool outputMatch)
  {
    int inputCount = Math.Max(requirement.InputParameters.Count, command.InputParameters.Count);
    double inputNameSimilarity = inputCount == 0 ? 0 : (double)matchingInputNameCount / inputCount;
    double inputSchemaSimilarity = inputCount == 0 ? 0 : (double)matchingInputCount / inputCount;

    int nameScore = !hasName ? 0 : (nameMatches ? 50 : (int)(NameSimilarity(requirement.Name, command.Name) * 30));

    // Assuming 25 and 15 weights from the original intent
    int inputNameScore = (int)(inputNameSimilarity * 25);
    int inputSchemaScore = (int)(inputSchemaSimilarity * 15);
    int outputScore = outputMatch ? 30 : 0;

    return nameScore + inputNameScore + inputSchemaScore + outputScore;
  }

  private static string BuildMatchDescription(
      CustomCommandRequirement requirement,
      CustomCommandVersion command,
      bool nameMatches,
      bool inputMatch,
      int matchingInputNameCount,
      int matchingInputCount,
      bool outputMatch)
  {
    string nameDesc = string.IsNullOrWhiteSpace(requirement.Name)
        ? "not captured"
        : nameMatches ? "matches" : "is similar";

    string inputDesc;
    if(inputMatch && requirement.InputParameters.Count == 0 && command.InputParameters.Count == 0)
      inputDesc = "neither command has input parameters";
    else if(inputMatch)
      inputDesc = $"matches all {requirement.InputParameters.Count} input parameters";
    else if(requirement.InputParameters.Count == 0)
      inputDesc = $"the imported command has no inputs; candidate has {command.InputParameters.Count} input parameter(s)";
    else if(command.InputParameters.Count == 0)
      inputDesc = $"matches {matchingInputNameCount} of {requirement.InputParameters.Count} input parameter names and {matchingInputCount} schemas; candidate has no inputs";
    else
      inputDesc = $"matches {matchingInputNameCount} of {requirement.InputParameters.Count} input parameter names and {matchingInputCount} schemas; candidate has {command.InputParameters.Count} input parameter(s)";

    string outputDesc = requirement.OutputSchema is null && command.OutputSchema is null
      ? "neither command has an output schema"
      : outputMatch ? "matches" : "differs";

    return $"Name {nameDesc}; input interface {inputDesc}; output interface {outputDesc}.";
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
    foreach(var dependency in analysis.DeviceDependencies)
      foreach(var command in GetExperimentTemplates(analysis.Template).SelectMany(experiment => experiment.StepTemplates).SelectMany(step => step.CommandTemplates))
        if(command.DeviceCommand?.Metadata is { } metadata && string.Equals(metadata.DeviceId, dependency.DeviceId, StringComparison.OrdinalIgnoreCase))
        {
          var commandDependency = dependency.Commands.FirstOrDefault(requirement => string.Equals(requirement.Name, metadata.Name, StringComparison.OrdinalIgnoreCase));
          if(!string.IsNullOrWhiteSpace(commandDependency?.SelectedReplacementCommandName))
            metadata.Name = commandDependency.SelectedReplacementCommandName!;
          if(!string.IsNullOrWhiteSpace(dependency.SelectedReplacementDeviceId))
            metadata.DeviceId = dependency.SelectedReplacementDeviceId!;
        }

    foreach(var dependency in analysis.CustomCommandDependencies.Where(dependency => !string.IsNullOrWhiteSpace(dependency.SelectedReplacementCustomCommandId)))
      foreach(var command in GetExperimentTemplates(analysis.Template).SelectMany(experiment => experiment.StepTemplates).SelectMany(step => step.CommandTemplates))
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

  public static IEnumerable<ExperimentTemplate> GetExperimentTemplates(CampaignTemplate template)
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

    var invalidCommand = CampaignTemplateHelper.GetExperimentTemplates(template)
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