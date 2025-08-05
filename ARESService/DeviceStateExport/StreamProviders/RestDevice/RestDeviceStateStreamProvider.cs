using Ares.Messages.DeviceStates.RestDevice;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.RestDevice;

public class RestDeviceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<RestDeviceStateEntity, RestDeviceStateMap>
{
  public RestDeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
