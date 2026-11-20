namespace Ares.Core.Campaigns.Models;

public sealed class CampaignDeviceCommandCandidate
{
  public required string RequiredName { get; init; }
  public required string Name { get; init; }
  public required CampaignCommandMatchStatus Status { get; init; }
  public int Score { get; init; }
  public string MatchDescription { get; init; } = string.Empty;
}