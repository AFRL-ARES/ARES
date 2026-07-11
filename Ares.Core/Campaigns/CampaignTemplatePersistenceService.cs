using Ares.Datamodel.Templates;
using Ares.Services;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Campaigns;

internal class CampaignTemplatePersistenceService(IDbContextFactory<CoreDatabaseContext> contextFactory)
  : ICampaignTemplatePersistenceService
{
  public async Task<IReadOnlyList<CampaignTemplateSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    return await context.CampaignTemplates
      .AsNoTracking()
      .IgnoreAutoIncludes()
      .OrderBy(template => template.Name)
      .Select(template => new CampaignTemplateSummary
      {
        UniqueId = template.UniqueId,
        CampaignName = template.Name
      })
      .ToArrayAsync(cancellationToken);
  }

  public async Task<CampaignTemplate?> GetByIdAsync(string uniqueId, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    return await QueryCampaigns(context, asNoTracking: true)
      .FirstOrDefaultAsync(template => template.UniqueId == uniqueId, cancellationToken);
  }

  public async Task<CampaignTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    return await QueryCampaigns(context, asNoTracking: true)
      .FirstOrDefaultAsync(template => template.Name == name, cancellationToken);
  }

  public async Task<bool> ExistsByIdAsync(string uniqueId, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    return await context.CampaignTemplates
      .IgnoreAutoIncludes()
      .AnyAsync(template => template.UniqueId == uniqueId, cancellationToken);
  }

  public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    return await context.CampaignTemplates
      .IgnoreAutoIncludes()
      .AnyAsync(template => template.Name == name, cancellationToken);
  }

  public async Task AddAsync(CampaignTemplate template, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    context.CampaignTemplates.Add(template);
    await context.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> ReplaceAsync(CampaignTemplate template, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var existingTemplate = await QueryCampaigns(context, asNoTracking: false)
      .FirstOrDefaultAsync(existing => existing.UniqueId == template.UniqueId, cancellationToken);
    if(existingTemplate is null)
      return false;

    RemoveCampaignGraph(context, existingTemplate);
    await context.SaveChangesAsync(cancellationToken);
    context.CampaignTemplates.Add(template);
    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return true;
  }

  public async Task<bool> DeleteAsync(string uniqueId, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    var template = await QueryCampaigns(context, asNoTracking: false)
      .FirstOrDefaultAsync(existing => existing.UniqueId == uniqueId, cancellationToken);
    if(template is null)
      return false;

    RemoveCampaignGraph(context, template);
    await context.SaveChangesAsync(cancellationToken);
    return true;
  }

  private static IQueryable<CampaignTemplate> QueryCampaigns(CoreDatabaseContext context, bool asNoTracking)
  {
    var query = context.CampaignTemplates.AsSplitQuery();
    return asNoTracking ? query.AsNoTracking() : query;
  }

  private static void RemoveCampaignGraph(CoreDatabaseContext context, CampaignTemplate template)
  {
    var experiments = new[]
      {
        template.StartupTemplate,
        template.ExperimentTemplate,
        template.CloseoutTemplate
      }
      .Where(experiment => experiment is not null)
      .DistinctBy(experiment => experiment!.UniqueId)
      .ToArray();
    context.ExperimentTemplates.RemoveRange(experiments!);
    context.CampaignTemplates.Remove(template);
  }
}
