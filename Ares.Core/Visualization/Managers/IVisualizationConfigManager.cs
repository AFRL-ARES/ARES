using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;

namespace Ares.Core.Visualization.Managers;

public interface IVisualizationConfigManager
{
  Task LoadConfigs();
  Task AddDeviceVisualization(string deviceId, VisualizationPath path, ChartStyle style);
  Task Remove(string configId);
  Task UpdateDeviceVisualization(string configId, DeviceVisualizationConfig config);
}
