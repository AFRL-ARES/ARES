namespace Ares.Core.Campaigns.Models;

public sealed class CampaignCustomCommandCandidate
{
  public required string CustomCommandId { get; init; }
  public required string Name { get; init; }
  public string Description { get; init; } = string.Empty;
  public required CampaignCommandMatchStatus Status { get; init; }
  public int Score { get; init; }
  public string MatchDescription { get; init; } = string.Empty;
}