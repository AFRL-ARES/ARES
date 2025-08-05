using Ares.Messages.DeviceStates.TicStepperController;
using AresService.DeviceStateExport.StateGetters;

namespace AresService.DeviceStateExport.StreamProviders.StepperController;

public class TicStepperControllerStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TicStepperControllerState, TicStepperControllerStateMap>
{
  public TicStepperControllerStateStreamProvider(IDeviceStateGetter deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
