using Ares.Datamodel.Visualizing.Local;

namespace Ares.Core.Visualization.Helpers;

public static class VisualizationConfigHelpers
{
  public static IEnumerable<string> GetAssociatedDeviceIds(this DeviceVisualizationConfig config)
    => config.Paths.Select(p => p.AssociatedDeviceId);
}
