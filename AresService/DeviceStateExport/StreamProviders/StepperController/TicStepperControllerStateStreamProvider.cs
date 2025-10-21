using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.TicStepperController;

namespace AresService.DeviceStateExport.StreamProviders.StepperController;

public class TicStepperControllerStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TicStepperControllerState, TicStepperControllerStateMap>
{
  public TicStepperControllerStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
