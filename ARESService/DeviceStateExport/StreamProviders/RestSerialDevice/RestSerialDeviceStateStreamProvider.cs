using Ares.Messages.DeviceStates.RestSerialDevice;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.RestSerialDevice;

public class RestSerialDeviceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<RestSerialDeviceStateEntity, RestSerialDeviceStateMap>
{
  public RestSerialDeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
