using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

public sealed class CampaignDeviceDependency
{
  public required string DeviceId { get; init; }
  public string DeviceType { get; init; } = string.Empty;
  public DeviceCategory DeviceCategory { get; init; }
  public string DriverId { get; init; } = string.Empty;
  public required IReadOnlyList<CampaignDeviceCommandDependency> Commands { get; init; }
  public required IReadOnlyList<CampaignDeviceCandidate> Candidates { get; init; }
  public required IReadOnlyList<CampaignCommandMatch> CurrentCommandMatches { get; init; }
  public IReadOnlyDictionary<string, IReadOnlyList<CampaignDeviceCommandCandidate>> CommandCandidates { get; set; } = new Dictionary<string, IReadOnlyList<CampaignDeviceCommandCandidate>>(StringComparer.OrdinalIgnoreCase);
  public string? SelectedReplacementDeviceId { get; set; }
  public bool IsResolved { get; init; }
  public bool HasCommandMismatches => CurrentCommandMatches.Any(match => match.Status != CampaignCommandMatchStatus.Exact);
  public bool RequiresRepair => (!IsResolved || HasCommandMismatches) && !string.IsNullOrWhiteSpace(DeviceId);
}