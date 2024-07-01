using Ares.Messages.DeviceStates.SyringePump;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders.SyringePump;

public class SyringePumpStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<SyringePumpState, SyringePumpStateMap>
{
  public SyringePumpStateStreamProvider(IDeviceStateGetter<SyringePumpState> stateGetter) : base(stateGetter)
  {
  }
}
