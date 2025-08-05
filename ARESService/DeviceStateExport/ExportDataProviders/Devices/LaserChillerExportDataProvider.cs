using Ares.Messages.DeviceStates.Chiller;
using AresService.DeviceStateExport.StateGetters;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders.Devices;

public class LaserChillerExportDataProvider : DeviceStateDataProviderBase<ChillerState>
{
  public LaserChillerExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {

  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<ChillerState> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Manifold Temperature (°C)", deviceName, d.ManifoldTemperature)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
