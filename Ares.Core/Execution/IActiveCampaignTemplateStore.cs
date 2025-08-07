using Ares.Datamodel.Templates;

namespace Ares.Core.Execution;

/// <summary>
/// Contains the currently active campaign template. Primarily used to decouple template
/// storage from the <see cref="IExecutionManager" /> so that other components would not need
/// to inject the whole execution manager when just needing the template itself.
/// </summary>
public interface IActiveCampaignTemplateStore
{
  public CampaignTemplate? CampaignTemplate { get; set; }
}
