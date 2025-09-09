using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.Mfc;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders;

public class MfcExportDataProvider : DeviceStateDataProviderBase<MfcState>
{
  public MfcExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<MfcState> mfcStates)
  {
    var exportItems = mfcStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Gas", deviceName, d.Gas),
          new("Mass Flow (SCCM)", deviceName, d.MassFlow),
          new("Temperature (°C)", deviceName, d.Temperature),
          new("Absolute Pressure (PSI)", deviceName, d.AbsolutePressure),
          new("Volumetric Flow (CCM)", deviceName, d.VolumetricFlow),
          new("Setpoint (SCCM)", deviceName, d.Setpoint),
          new("Status Codes", deviceName, d.StatusCodes)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
