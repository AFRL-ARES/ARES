using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.TubeFurnace;

namespace AresService.DeviceStateExport.StreamProviders.TubeFurnace;

public class TubeFurnaceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TubeFurnaceStateEntity, TubeFurnaceStateMap>
{
  public TubeFurnaceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
