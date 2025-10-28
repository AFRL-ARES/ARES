using System.Collections.Generic;
using System.Linq;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.RestSerialDevice;

namespace AresService.DeviceStateExport.ExportDataProviders;

public class RestSerialDeviceExportDataProvider : DeviceStateDataProviderBase<RestSerialDeviceStateEntity>
{
  public RestSerialDeviceExportDataProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<RestSerialDeviceStateEntity> deviceStates)
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
