using Ares.Messages.DeviceStates.Tc0304;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders.Tc0304;

public class Tc0304StateStreamProvider : SingleDeviceTypeStateStreamProviderBase<Tc0304State, Tc0304StateMap>
{
  public Tc0304StateStreamProvider(IDeviceStateGetter<Tc0304State> stateGetter) : base(stateGetter)
  {
  }
}
