using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;

namespace Ares.Core.Visualization.Managers;

public interface IVisualizationConfigManager
{
  Task Initialize();
  Task AddDeviceVisualization(List<VisualizationPath> paths, ChartStyle style);
  Task Remove(string configId);
  Task UpdateDeviceVisualization(string configId, DeviceVisualizationConfig config);
}
