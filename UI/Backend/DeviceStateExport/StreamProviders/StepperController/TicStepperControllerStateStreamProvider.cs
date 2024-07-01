using Ares.Messages.DeviceStates.TicStepperController;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.StreamProviders.StepperController;

public class TicStepperControllerStateStreamProvider : SingleDeviceTypeStateStreamProviderBase<TicStepperControllerState, TicStepperControllerStateMap>
{
  public TicStepperControllerStateStreamProvider(IDeviceStateGetter<TicStepperControllerState> deviceStateGetter) : base(deviceStateGetter)
  {
  }
}
