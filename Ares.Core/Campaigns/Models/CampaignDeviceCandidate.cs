using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

public sealed class CampaignDeviceCandidate
{
  public required string DeviceId { get; init; }
  public string Name { get; init; } = string.Empty;
  public string Type { get; init; } = string.Empty;
  public DeviceCategory Category { get; init; }
  public string DriverId { get; init; } = string.Empty;
  public int Score { get; init; }
  public required IReadOnlyList<CampaignCommandMatch> CommandMatches { get; init; }
  public IReadOnlyDictionary<string, IReadOnlyList<CampaignDeviceCommandCandidate>> CommandCandidates { get; init; } = new Dictionary<string, IReadOnlyList<CampaignDeviceCommandCandidate>>(StringComparer.OrdinalIgnoreCase);
  public string MatchDescription => string.Join("; ", CommandMatches
    .Select(match => match.Description)
    .Where(description => !string.IsNullOrWhiteSpace(description))
    .Distinct(StringComparer.OrdinalIgnoreCase));
}