using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.TicStepperController;

namespace UI.Backend.DeviceStateExport.StateGetters;

public class StepperControllerStateGetter : IDeviceStateGetter<TicStepperControllerState>
{
  readonly TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient _client;
  public StepperControllerStateGetter(TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient client)
  {
    _client = client;
  }

  public async Task<IDictionary<string, IEnumerable<TicStepperControllerState>>> GetStates(StateRequest request)
  {
    var stateResponse = await _client.GetTicStepperControllerStatesAsync(request);
    var stateMap = stateResponse.StateMap.ToDictionary(k => k.Key, v => v.Value.StateLogs.AsEnumerable());
    return stateMap;
  }
}
