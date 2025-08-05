using Ares.Messages.DeviceStates.SyringePump;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.SyringePump;

public class SyringePumpStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<SyringePumpState, SyringePumpStateMap>
{
  public SyringePumpStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }
}
