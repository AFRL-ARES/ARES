using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

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