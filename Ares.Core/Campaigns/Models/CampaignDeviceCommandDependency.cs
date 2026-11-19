using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

public sealed class CampaignDeviceCommandDependency
{
  public required string CommandId { get; init; }
  public required string Name { get; init; }
  public required IReadOnlyList<ParameterMetadata> Parameters { get; init; }
  public AresValueSchema? OutputSchema { get; init; }
  public required IReadOnlyList<string> Locations { get; init; }
  public IReadOnlyList<CampaignDeviceCommandCandidate> Candidates { get; set; } = [];
  public string? SelectedReplacementCommandName { get; set; }
}