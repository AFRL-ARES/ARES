using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.Chiller;

namespace AresService.DeviceStateExport.StreamProviders.LaserChiller;

public class ChillerStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<ChillerState, ChillerStateMap>
{
  public ChillerStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {

  }
}
