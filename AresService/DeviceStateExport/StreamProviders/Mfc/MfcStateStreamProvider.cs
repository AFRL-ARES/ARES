using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.Mfc;

namespace AresService.DeviceStateExport.StreamProviders.Mfc;

public class MfcStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<MfcState, MfcStateMap>
{
  public MfcStateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }
}
