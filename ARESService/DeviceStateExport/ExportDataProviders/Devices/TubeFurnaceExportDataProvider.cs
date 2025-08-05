using Ares.Messages.DeviceStates.TubeFurnace;
using AresService.DeviceStateExport.StateGetters;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders.Devices;

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

