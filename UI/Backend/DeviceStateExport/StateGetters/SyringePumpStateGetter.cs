using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.SyringePump;

namespace UI.Backend.DeviceStateExport.StateGetters;

public class SyringePumpStateGetter : IDeviceStateGetter<SyringePumpState>
{
  readonly SyringePumpStateLogging.SyringePumpStateLoggingClient _client;
  public SyringePumpStateGetter(SyringePumpStateLogging.SyringePumpStateLoggingClient client)
  {
    _client = client;
  }

  public async Task<IDictionary<string, IEnumerable<SyringePumpState>>> GetStates(StateRequest request)
  {
    var stateResponse = await _client.GetSyringePumpStatesAsync(request);
    var stateMap = stateResponse.StateMap.ToDictionary(k => k.Key, v => v.Value.StateLogs.AsEnumerable());
    return stateMap;
  }
}
