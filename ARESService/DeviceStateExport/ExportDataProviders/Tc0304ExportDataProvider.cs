using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.Tc0304;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders;

public class Tc0304ExportDataProvider : DeviceStateDataProviderBase<Tc0304State>
{
  public Tc0304ExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(
    string deviceName,
    IEnumerable<Tc0304State> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Probe 1 Temperature (°C)", deviceName, d.Probe1Temperature),
          new("Probe 2 Temperature (°C)", deviceName, d.Probe2Temperature),
          new("Probe 3 Temperature (°C)", deviceName, d.Probe3Temperature),
          new("Probe 4 Temperature (°C)", deviceName, d.Probe4Temperature)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
