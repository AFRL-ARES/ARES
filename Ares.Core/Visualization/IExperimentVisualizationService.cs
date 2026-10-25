using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Visualization;

public interface IExperimentVisualizationService
{
  Task GenerateVisualizations(
    ExperimentTemplate experimentTemplate,
    IEnumerable<ExperimentExecutionSummary> completedExperiments,
    ExperimentExecutionSummary currentExperiment,
    CancellationToken cancellationToken = default);
}