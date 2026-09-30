using Ares.Datamodel.Templates;
using Ares.Services;

namespace Ares.Core.Campaigns;

/// <summary>
/// Demo-mode implementation of <see cref=\"ICampaignTemplatePersistenceService\"/>.
/// This service keeps campaign templates in memory only and never touches the
/// database. It seeds the system with a single demo campaign template.
/// </summary>
internal class DemoCampaignTemplatePersistenceService : ICampaignTemplatePersistenceService
{
  private readonly List<CampaignTemplate> _templates = new();

  public DemoCampaignTemplatePersistenceService()
  {
    // Seed with a single demo campaign template.
    var demoTemplate = DemoCampaignTemplateFactory.Create();
    _templates.Add(demoTemplate);
  }

  public Task<IReadOnlyList<CampaignTemplateSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
  {
    var summaries = _templates
      .OrderBy(template => template.Name)
      .Select(template => new CampaignTemplateSummary
      {
        UniqueId = template.UniqueId,
        CampaignName = template.Name
      })
      .ToArray();

    return Task.FromResult<IReadOnlyList<CampaignTemplateSummary>>(summaries);
  }

  public Task<CampaignTemplate?> GetByIdAsync(string uniqueId, CancellationToken cancellationToken = default)
    => Task.FromResult(_templates.FirstOrDefault(template => template.UniqueId == uniqueId));

  public Task<CampaignTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    => Task.FromResult(_templates.FirstOrDefault(template => template.Name == name));

  public Task<bool> ExistsByIdAsync(string uniqueId, CancellationToken cancellationToken = default)
    => Task.FromResult(_templates.Any(template => template.UniqueId == uniqueId));

  public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    => Task.FromResult(_templates.Any(template => template.Name == name));

  public Task AddAsync(CampaignTemplate template, CancellationToken cancellationToken = default)
  {
    // In demo mode we keep templates purely in memory. Allow users to add
    // additional demo templates for the session without persisting them.
    if(!_templates.Any(t => t.UniqueId == template.UniqueId))
    {
      _templates.Add(template);
    }

    return Task.CompletedTask;
  }

  public Task<bool> ReplaceAsync(CampaignTemplate template, CancellationToken cancellationToken = default)
  {
    var existingIndex = _templates.FindIndex(t => t.UniqueId == template.UniqueId);
    if(existingIndex < 0)
      return Task.FromResult(false);

    _templates[existingIndex] = template;
    return Task.FromResult(true);
  }

  public Task<bool> DeleteAsync(string uniqueId, CancellationToken cancellationToken = default)
  {
    var existingIndex = _templates.FindIndex(t => t.UniqueId == uniqueId);
    if(existingIndex < 0)
      return Task.FromResult(false);

    _templates.RemoveAt(existingIndex);
    return Task.FromResult(true);
  }
}
