using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.Remote.State;
public class DeviceStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<DeviceState, RemoteDeviceStateMap>
{
  public DeviceStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
