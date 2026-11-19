namespace Ares.Core.Campaigns.Models;

public sealed class CampaignCommandMatch
{
  public required string RequiredName { get; init; }
  public string? CurrentName { get; init; }
  public required CampaignCommandMatchStatus Status { get; init; }
  public string Description { get; init; } = string.Empty;
}