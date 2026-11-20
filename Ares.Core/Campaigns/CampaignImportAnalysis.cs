using Ares.Core.Campaigns.Models;
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