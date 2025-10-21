using System.Collections.Generic;
using System.Linq;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.Chiller;

namespace AresService.DeviceStateExport.ExportDataProviders;

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
