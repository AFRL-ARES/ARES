using Ares.Messages.DeviceStates.Mfc;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders.Mfc;

public class MfcStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<MfcState, MfcStateMap>
{
  public MfcStateStreamProvider(IDeviceStateGetter<MfcState> stateGetter) : base(stateGetter)
  {
  }
}
