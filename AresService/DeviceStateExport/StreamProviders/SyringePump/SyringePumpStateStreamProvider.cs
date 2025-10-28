using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.SyringePump;

namespace AresService.DeviceStateExport.StreamProviders.SyringePump;

public class SyringePumpStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<SyringePumpState, SyringePumpStateMap>
{
  public SyringePumpStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }
}
