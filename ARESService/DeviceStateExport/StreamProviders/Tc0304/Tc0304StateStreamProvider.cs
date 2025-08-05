using Ares.Messages.DeviceStates.Tc0304;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.Tc0304;

public class Tc0304StateStreamProvider : SingleDeviceTypeStateStreamProviderBase<Tc0304State, Tc0304StateMap>
{
  public Tc0304StateStreamProvider(IDeviceStateGetter stateGetter) : base(stateGetter)
  {
  }
}
