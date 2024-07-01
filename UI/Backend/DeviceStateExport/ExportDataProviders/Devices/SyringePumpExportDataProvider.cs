using Ares.Messages.DeviceStates.SyringePump;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders.Devices;

public class SyringePumpExportDataProvider : DeviceStateDataProviderBase<SyringePumpState>
{
  public SyringePumpExportDataProvider(IDeviceStateGetter<SyringePumpState> stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<SyringePumpState> deviceStates)
  {
    var exportItems = deviceStates.Select(d =>
    {
      var itemsAtUniqueTimestamp = new StateExportItem[]
      {
        new StateExportItem("Rate Unit", deviceName, d.RateUnit),
        new StateExportItem("Volume Unit", deviceName, d.VolumeUnit),
        new StateExportItem("Address", deviceName, d.Address),
        new StateExportItem("Dispensed Volume", deviceName, d.DispensedVolume),
        new StateExportItem("Withdrawn Volume", deviceName, d.WithdrawnVolume),
        new StateExportItem("Status", deviceName, d.Status)
      };

      return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
    });

    return exportItems;
  }
}
