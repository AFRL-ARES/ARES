using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns;

public sealed class CampaignImportAnalysis
{
  public required CampaignTemplate Template { get; init; }
  public required IReadOnlyList<string> Warnings { get; init; }
  public required IReadOnlyList<CampaignDeviceDependency> DeviceDependencies { get; init; }
  public required IReadOnlyList<CampaignCustomCommandDependency> CustomCommandDependencies { get; init; }

  public bool HasRepairs => DeviceDependencies.Any(dependency => dependency.RequiresRepair)
    || CustomCommandDependencies.Any(dependency => dependency.RequiresRepair);
}

public sealed class CampaignDeviceDependency
{
  public required string DeviceId { get; init; }
  public string DeviceType { get; init; } = string.Empty;
  public DeviceCategory DeviceCategory { get; init; }
  public string DriverId { get; init; } = string.Empty;
  public required IReadOnlyList<CampaignDeviceCommandDependency> Commands { get; init; }
  public required IReadOnlyList<CampaignDeviceCandidate> Candidates { get; init; }
  public required IReadOnlyList<CampaignCommandMatch> CurrentCommandMatches { get; init; }
  public string? SelectedReplacementDeviceId { get; set; }
  public bool IsResolved { get; init; }
  public bool HasCommandMismatches => CurrentCommandMatches.Any(match => match.Status != CampaignCommandMatchStatus.Exact);
  public bool RequiresRepair => (!IsResolved || HasCommandMismatches) && !string.IsNullOrWhiteSpace(DeviceId);
}

public sealed class CampaignDeviceCommandDependency
{
  public required string CommandId { get; init; }
  public required string Name { get; init; }
  public required IReadOnlyList<ParameterMetadata> Parameters { get; init; }
  public AresValueSchema? OutputSchema { get; init; }
  public required IReadOnlyList<string> Locations { get; init; }
}

public sealed class CampaignDeviceCandidate
{
  public required string DeviceId { get; init; }
  public string Name { get; init; } = string.Empty;
  public string Type { get; init; } = string.Empty;
  public DeviceCategory Category { get; init; }
  public string DriverId { get; init; } = string.Empty;
  public int Score { get; init; }
  public required IReadOnlyList<CampaignCommandMatch> CommandMatches { get; init; }
  public string MatchDescription => string.Join("; ", CommandMatches.Select(match => match.Description));
}

public sealed class CampaignCommandMatch
{
  public required string RequiredName { get; init; }
  public string? CurrentName { get; init; }
  public required CampaignCommandMatchStatus Status { get; init; }
  public string Description { get; init; } = string.Empty;
}

public enum CampaignCommandMatchStatus
{
  Exact,
  Compatible,
  SchemaMismatch,
  Missing
}

public sealed class CampaignCustomCommandDependency
{
  public required string CustomCommandId { get; init; }
  public required CustomCommandRequirement Requirement { get; init; }
  public required IReadOnlyList<string> Locations { get; init; }
  public required IReadOnlyList<CampaignCustomCommandCandidate> Candidates { get; init; }
  public bool IsResolved { get; init; }
  public bool CurrentSignatureMatches { get; init; }
  public string? SelectedReplacementCustomCommandId { get; set; }
  public bool RequiresRepair => (!IsResolved || !CurrentSignatureMatches) && !string.IsNullOrWhiteSpace(CustomCommandId);
}

public sealed class CampaignCustomCommandCandidate
{
  public required string CustomCommandId { get; init; }
  public required string Name { get; init; }
  public string Description { get; init; } = string.Empty;
  public required CampaignCommandMatchStatus Status { get; init; }
  public int Score { get; init; }
  public string MatchDescription { get; init; } = string.Empty;
}
