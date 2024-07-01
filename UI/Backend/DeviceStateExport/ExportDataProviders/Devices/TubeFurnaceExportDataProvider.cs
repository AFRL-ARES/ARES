using Ares.Messages.DeviceStates.TubeFurnace;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders.Devices;

public class TubeFurnaceExportDataProvider : DeviceStateDataProviderBase<TubeFurnaceStateEntity>
{
  public TubeFurnaceExportDataProvider(IDeviceStateGetter<TubeFurnaceStateEntity> stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<TubeFurnaceStateEntity> deviceStates)
  {
    var exportItems = deviceStates.Select(d =>
    {
      var itemsAtUniqueTimestamp = new StateExportItem[]
      {
      };

      return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
    });

    return exportItems;
  }
}

