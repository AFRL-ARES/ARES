using DynamicData;

namespace Ares.Core.Visualization.Providers;

public interface IRemoteVisualizationDataProvider
{
  IObservable<IChangeSet<LatestRemoteVisualization, string>> Connect();
  IReadOnlyCollection<LatestRemoteVisualization> GetAll();
  LatestRemoteVisualization? Get(string visualizerId, string requestedVisual);
  Task Initialize();
}