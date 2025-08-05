using Ares.Messages.DeviceStates.RestDevice;
using AresService.DeviceStateExport.StateGetters;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders.Devices;

public class RestDeviceExportDataProvider : DeviceStateDataProviderBase<RestDeviceStateEntity>
{
  public RestDeviceExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<RestDeviceStateEntity> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Acquired Values", deviceName, d.Values)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });
    return exportItems;
  }
}
