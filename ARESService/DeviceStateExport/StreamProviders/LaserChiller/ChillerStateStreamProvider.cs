using Ares.Messages.DeviceStates.Chiller;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.LaserChiller;

public class ChillerStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<ChillerState, ChillerStateMap>
{
  public ChillerStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {

  }
}
