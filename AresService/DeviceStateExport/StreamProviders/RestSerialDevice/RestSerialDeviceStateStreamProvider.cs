using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.RestSerialDevice;

namespace AresService.DeviceStateExport.StreamProviders.RestSerialDevice;

public class RestSerialDeviceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<RestSerialDeviceStateEntity, RestSerialDeviceStateMap>
{
  public RestSerialDeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
