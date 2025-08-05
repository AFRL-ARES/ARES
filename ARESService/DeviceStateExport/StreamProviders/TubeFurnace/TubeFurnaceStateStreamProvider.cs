using Ares.Messages.DeviceStates.TubeFurnace;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.TubeFurnace;

public class TubeFurnaceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TubeFurnaceStateEntity, TubeFurnaceStateMap>
{
  public TubeFurnaceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
