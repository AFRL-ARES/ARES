using Ares.Messages.DeviceStates.Mfc;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.Mfc;

public class MfcStateStreamProvider : StreamProviders.SingleDeviceTypeStateStreamProviderBase<MfcState, MfcStateMap>
{
  public MfcStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }
}
