using Ares.Messages.DeviceStates.Tc0304;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders.Devices;

public class Tc0304ExportDataProvider : DeviceStateDataProviderBase<Tc0304State>
{
  public Tc0304ExportDataProvider(IDeviceStateGetter<Tc0304State> stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<Tc0304State> deviceStates)
  {
    var exportItems = deviceStates.Select(d =>
    {
      var itemsAtUniqueTimestamp = new StateExportItem[]
      {
        new StateExportItem("Probe 1 Temperature (°C)", deviceName, d.Probe1Temperature),
        new StateExportItem("Probe 2 Temperature (°C)", deviceName, d.Probe2Temperature),
        new StateExportItem("Probe 3 Temperature (°C)", deviceName, d.Probe3Temperature),
        new StateExportItem("Probe 4 Temperature (°C)", deviceName, d.Probe4Temperature)
      };

      return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
    });

    return exportItems;
  }
}
