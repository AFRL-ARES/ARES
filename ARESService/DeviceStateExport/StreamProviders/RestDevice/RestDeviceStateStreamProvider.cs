using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.RestDevice;

namespace AresService.DeviceStateExport.StreamProviders.RestDevice;

public class RestDeviceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<RestDeviceStateEntity, RestDeviceStateMap>
{
  public RestDeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
