using Ares.Messages.DeviceStates;
using ARESMessaging.DeviceStateLogging;

namespace UI.Backend.DeviceStateExport.StateGetters;

/// <summary>
/// This interface is responsible for getting the raw device states from the backend service
/// It's usually the first step in the state export
/// </summary>
public interface IDeviceStateGetter<TState> where TState : IDeviceState
{
  /// <summary>
  /// Based on a state request, returns raw device states per device id
  /// </summary>
  Task<IDictionary<string, IEnumerable<TState>>> GetStates(StateRequest request);
}
