using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Visualizing.Remote;

namespace Ares.Core.Visualization;

public interface IRemoteVisualizer
{
  string Name { get; set; }
  string Type { get; }
  string Version { get; }
  string UniqueId { get; internal set; }
  string Description { get; }
  Uri Address { get; }
  State VisualizerState { get; }
  string StateMessage { get; }
  IObservable<State> VisualizerStateObservable { get; }
  AresStruct Settings { get; }
  IReadOnlyList<VisualizationOption> VisualizationOptions { get; }
  Task Init();
  Task Refresh();
  void UpdateSettings(AresStruct settings);
  Task<VisualizationOptionsResponse> GetVisualizationOptions(CancellationToken cancellationToken = default);
  Task<GetUpdatedVisualResponse> RequestUpdatedVisualization(GetUpdatedVisualRequest request, CancellationToken cancellationToken = default);
  IAsyncEnumerable<GetUpdatedVisualResponse> StreamPlotlyVisualization(GetUpdatedVisualRequest request, CancellationToken cancellationToken = default);
  Task<ConnectionStatus> GetConnectionStatus(CancellationToken cancellationToken = default);
}