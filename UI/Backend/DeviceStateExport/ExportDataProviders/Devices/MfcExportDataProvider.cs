using Ares.Messages.DeviceStates.Mfc;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders.Devices;

public class MfcExportDataProvider : DeviceStateDataProviderBase<MfcState>
{
  public MfcExportDataProvider(IDeviceStateGetter<MfcState> stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<MfcState> mfcStates)
  {
    var exportItems = mfcStates.Select(d =>
    {
      var itemsAtUniqueTimestamp = new StateExportItem[]
      {
        new StateExportItem("Gas", deviceName, d.Gas),
        new StateExportItem("Mass Flow (SCCM)", deviceName, d.MassFlow),
        new StateExportItem("Temperature (°C)", deviceName, d.Temperature),
        new StateExportItem("Absolute Pressure (PSI)", deviceName, d.AbsolutePressure),
        new StateExportItem("Volumetric Flow (CCM)", deviceName, d.VolumetricFlow),
        new StateExportItem("Setpoint (SCCM)", deviceName, d.Setpoint),
        new StateExportItem("Status Codes", deviceName, d.StatusCodes)
      };

      return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
    });

    return exportItems;
  }
}
