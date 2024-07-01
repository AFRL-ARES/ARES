using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Mfc;

namespace UI.Backend.DeviceStateExport.StateGetters;

public class MfcStateGetter : IDeviceStateGetter<MfcState>
{
  readonly MfcStateLogging.MfcStateLoggingClient _client;
  public MfcStateGetter(MfcStateLogging.MfcStateLoggingClient client)
  {
    _client = client;
  }

  public async Task<IDictionary<string, IEnumerable<MfcState>>> GetStates(StateRequest request)
  {
    var stateResponse = await _client.GetMfcStatesAsync(request);
    var stateMap = stateResponse.StateMap.ToDictionary(k => k.Key, v => v.Value.StateLogs.AsEnumerable());
    return stateMap;
  }
}
