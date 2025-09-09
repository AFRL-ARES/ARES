using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.TubeFurnace;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders;

public class TubeFurnaceExportDataProvider : DeviceStateDataProviderBase<TubeFurnaceStateEntity>
{
  public TubeFurnaceExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(
    string deviceName,
    IEnumerable<TubeFurnaceStateEntity> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Current Temperature (°C)", deviceName, d.CurrentTemp),
          new("Current Temperature Setpoint (°C)", deviceName, d.SetPointTemp)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}

