using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.TubeFurnace;

namespace UI.Backend.DeviceStateExport.StateGetters;

public class TubeFurnaceStateGetter : IDeviceStateGetter<TubeFurnaceStateEntity>
{
  readonly TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient _client;
  public TubeFurnaceStateGetter(TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient client)
  {
    _client = client;
  }

  public async Task<IDictionary<string, IEnumerable<TubeFurnaceStateEntity>>> GetStates(StateRequest request)
  {
    var stateResponse = await _client.GetTubeFurnaceStatesAsync(request);
    var stateMap = stateResponse.StateMap.ToDictionary(k => k.Key, v => v.Value.StateLogs.AsEnumerable());
    return stateMap;
  }
}
