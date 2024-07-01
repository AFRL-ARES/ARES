using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Tc0304;

namespace UI.Backend.DeviceStateExport.StateGetters;

public class Tc0304StateGetter : IDeviceStateGetter<Tc0304State>
{
  readonly Tc0304StateLogging.Tc0304StateLoggingClient _client;
  public Tc0304StateGetter(Tc0304StateLogging.Tc0304StateLoggingClient client)
  {
    _client = client;
  }

  public async Task<IDictionary<string, IEnumerable<Tc0304State>>> GetStates(StateRequest request)
  {
    var stateResponse = await _client.GetStatesAsync(request);
    var stateMap = stateResponse.StateMap.ToDictionary(k => k.Key, v => v.Value.StateLogs.AsEnumerable());
    return stateMap;
  }
}
