using Ares.Messages.DeviceStates.SyringePump;
using AresService.DeviceStateExport.StateGetters;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders.Devices;

public class SyringePumpExportDataProvider : DeviceStateDataProviderBase<SyringePumpState>
{
  public SyringePumpExportDataProvider(IDeviceStateGetter stateGetter) : base(
    stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(
    string deviceName,
    IEnumerable<SyringePumpState> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Rate Unit", deviceName, d.RateUnit),
          new("Volume Unit", deviceName, d.VolumeUnit),
          new("Address", deviceName, d.Address),
          new("Dispensed Volume", deviceName, d.DispensedVolume),
          new("Withdrawn Volume", deviceName, d.WithdrawnVolume),
          new("Status", deviceName, d.Status)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
