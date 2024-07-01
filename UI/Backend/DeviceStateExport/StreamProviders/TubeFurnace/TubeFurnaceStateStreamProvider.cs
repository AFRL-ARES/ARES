using Ares.Messages.DeviceStates.TubeFurnace;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders.TubeFurnace;

public class TubeFurnaceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TubeFurnaceStateEntity, TubeFurnaceStateMap>
{
  public TubeFurnaceStateStreamProvider(IDeviceStateGetter<TubeFurnaceStateEntity> deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
