using Ares.Core.Execution;
using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using DynamicData;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Visualization.Providers;

public sealed class RemoteVisualizationDataProvider : IRemoteVisualizationDataProvider, IDisposable
{
  private const char SourceKeySeparator = '\u001f';
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly SourceCache<LatestRemoteVisualization, string> _latestVisualizations = new(item => item.SourceKey);
  private readonly IDisposable _executionSubscription;

  public RemoteVisualizationDataProvider(IDbContextFactory<CoreDatabaseContext> dbContextFactory, IExecutionReportStore executionReportStore)
  {
    _dbContextFactory = dbContextFactory;
    _executionReportStore = executionReportStore;
    _executionSubscription = _executionReportStore.CompletedExperimentObservable.Subscribe(ProcessSummary);
  }

  public IObservable<IChangeSet<LatestRemoteVisualization, string>> Connect()
    => _latestVisualizations.Connect();

  public IReadOnlyCollection<LatestRemoteVisualization> GetAll()
    => _latestVisualizations.Items.ToList().AsReadOnly();

  public LatestRemoteVisualization? Get(string visualizerId, string requestedVisual)
  {
    var lookup = _latestVisualizations.Lookup(CreateSourceKey(visualizerId, requestedVisual));
    return lookup.HasValue ? lookup.Value : null;
  }

  public async Task Initialize()
  {
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var summaries = await context.ExperimentExecutionSummaries
      .AsNoTracking()
      .IgnoreAutoIncludes()
      .OrderByDescending(summary => summary.ExecutionInfo.TimeFinished)
      .ToArrayAsync();

    foreach(var summary in summaries)
      ProcessSummary(summary);
  }

  private void ProcessSummary(ExperimentExecutionSummary? summary)
  {
    if(summary is null || summary.GeneratedVisuals.Count == 0)
      return;

    var allocations = summary.ExperimentOverview?.Template?.VisualizerAllocations
      .Where(allocation => !string.IsNullOrWhiteSpace(allocation.UniqueId))
      .ToDictionary(allocation => allocation.UniqueId, StringComparer.Ordinal)
      ?? new Dictionary<string, VisualizerAllocation>(StringComparer.Ordinal);

    var generatedAt = summary.ExecutionInfo?.TimeFinished?.ToDateTime() ?? DateTime.MinValue;
    foreach(var generatedVisual in summary.GeneratedVisuals)
    {
      if(!allocations.TryGetValue(generatedVisual.Key, out var allocation))
        continue;

      if(allocation.Visualizer is null ||
         string.IsNullOrWhiteSpace(allocation.Visualizer.UniqueId) ||
         string.IsNullOrWhiteSpace(allocation.RequestedVisual) ||
         string.IsNullOrWhiteSpace(generatedVisual.Value))
        continue;

      var sourceKey = CreateSourceKey(allocation.Visualizer.UniqueId, allocation.RequestedVisual);
      var existing = _latestVisualizations.Lookup(sourceKey);
      if(existing.HasValue && existing.Value.GeneratedAt > generatedAt)
        continue;

      _latestVisualizations.AddOrUpdate(new LatestRemoteVisualization(
        sourceKey,
        allocation.Visualizer.UniqueId,
        allocation.RequestedVisual,
        generatedVisual.Value,
        generatedAt,
        allocation.Visualizer.Name));
    }
  }

  private static string CreateSourceKey(string visualizerId, string requestedVisual)
    => $"{visualizerId}{SourceKeySeparator}{requestedVisual}";

  public void Dispose()
  {
    _executionSubscription.Dispose();
    _latestVisualizations.Dispose();
    GC.SuppressFinalize(this);
  }
}