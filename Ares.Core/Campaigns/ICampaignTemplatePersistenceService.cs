using Ares.Datamodel.Templates;
using Ares.Services;

namespace Ares.Core.Campaigns;

public interface ICampaignTemplatePersistenceService
{
  Task<IReadOnlyList<CampaignTemplateSummary>> GetSummariesAsync(CancellationToken cancellationToken = default);

  Task<CampaignTemplate?> GetByIdAsync(string uniqueId, CancellationToken cancellationToken = default);

  Task<CampaignTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

  Task<bool> ExistsByIdAsync(string uniqueId, CancellationToken cancellationToken = default);

  Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

  Task AddAsync(CampaignTemplate template, CancellationToken cancellationToken = default);

  Task<bool> ReplaceAsync(CampaignTemplate template, CancellationToken cancellationToken = default);

  Task<bool> DeleteAsync(string uniqueId, CancellationToken cancellationToken = default);
}
