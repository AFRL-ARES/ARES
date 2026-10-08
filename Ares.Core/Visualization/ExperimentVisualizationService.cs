using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Remote;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Visualization;

public sealed class ExperimentVisualizationService(IVisualizerRepo visualizerRepo, INotifier notifier, ILogger<ExperimentVisualizationService> logger) : IExperimentVisualizationService
{
  private readonly IVisualizerRepo _visualizerRepo = visualizerRepo;
  private readonly INotifier _notifier = notifier;
  private readonly ILogger<ExperimentVisualizationService> _logger = logger;

  public async Task GenerateVisualizations(
    ExperimentTemplate experimentTemplate, 
    IEnumerable<ExperimentExecutionSummary> completedExperiments, 
    ExperimentExecutionSummary currentExperiment,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(experimentTemplate);
    ArgumentNullException.ThrowIfNull(currentExperiment);

    var summaries = completedExperiments.Append(currentExperiment).ToArray();
    foreach(var allocation in experimentTemplate.VisualizerAllocations)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        await GenerateVisualization(allocation, summaries, currentExperiment, cancellationToken);
      }
      catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch(Exception exception)
      {
        var visualName = string.IsNullOrWhiteSpace(allocation.RequestedVisual) ? "unknown visualization" : allocation.RequestedVisual;
        var message = $"Failed to generate {visualName}: {exception.Message}";

        _logger.LogWarning(exception, "{Message}", message);
        await _notifier.Notify("Visualization Generation Failed", message, NotificationSeverityEnum.Warning);
      }
    }
  }

  private async Task GenerateVisualization(
    VisualizerAllocation allocation,
    IReadOnlyList<ExperimentExecutionSummary> summaries,
    ExperimentExecutionSummary currentExperiment,
    CancellationToken cancellationToken)
  {
    if(allocation.Visualizer is null || string.IsNullOrWhiteSpace(allocation.Visualizer.UniqueId))
      throw new InvalidOperationException("The visualization allocation does not identify a visualizer.");

    if(string.IsNullOrWhiteSpace(allocation.RequestedVisual))
      throw new InvalidOperationException("The visualization allocation does not identify a requested visualization.");

    var visualizer = _visualizerRepo.GetVisualizerById(allocation.Visualizer.UniqueId);
    if(visualizer is null)
      throw new InvalidOperationException($"Visualizer '{allocation.Visualizer.UniqueId}' was not found.");

    var request = new GetUpdatedVisualRequest 
    {
      VisualizationName = allocation.RequestedVisual
    };

    for(var index = 0; index < summaries.Count; index++)
    {
      request.VisualizationData.Add(new VisualizationDataPoint
      {
        Index = index,
        Values = ResolveVisualizationValues(summaries[index], allocation)
      });
    }

    var response = await visualizer.RequestUpdatedVisualization(request, cancellationToken);
    var visualKey = GetVisualKey(allocation);
    currentExperiment.GeneratedVisuals[visualKey] = response.ChartJsonData;
  }

  private static AresStruct ResolveVisualizationValues(ExperimentExecutionSummary summary, VisualizerAllocation allocation)
  {
    var availableValues = new Dictionary<string, AresValue>();

    foreach(var field in summary.ExperimentOverview?.Result?.Fields ?? [])
      AddFlattenedValue(availableValues, field.Key, field.Value);

    foreach(var objective in summary.ExperimentOverview?.AnalysisOverview?.Objectives ?? [])
    {
      if(objective.ObjectiveValue is not null)
        AddFlattenedValue(availableValues, objective.ObjectiveName, objective.ObjectiveValue);
    }

    var values = new AresStruct();
    foreach(var mapping in allocation.VisualizationMaps)
    {
      if(!availableValues.TryGetValue(mapping.Value, out var value))
        throw new InvalidOperationException($"Output '{mapping.Value}' could not be found for visualization input '{mapping.Key}'.");

      values.Fields[mapping.Key] = value.Clone();
    }

    return values;
  }

  private static void AddFlattenedValue(IDictionary<string, AresValue> values, string path, AresValue value)
  {
    values[path] = value;

    if(value.GetAresDataType() != AresDataType.Struct || value.StructValue is null)
      return;

    foreach(var field in value.StructValue.Fields)
      AddFlattenedValue(values, $"{path}.{field.Key}", field.Value);
  }

  private static string GetVisualKey(VisualizerAllocation allocation)
  {
    if(!string.IsNullOrWhiteSpace(allocation.UniqueId))
      return allocation.UniqueId;

    return $"{allocation.Visualizer.UniqueId}:{allocation.RequestedVisual}";
  }
}