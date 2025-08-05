using System.Collections.Generic;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using AresMessaging.DeviceStateLogging;

namespace AresService.DeviceStateExport.StateGetters;

/// <summary>
/// This interface is responsible for getting the raw device states from the backend service
/// It's usually the first step in the state export
/// </summary>
public interface IDeviceStateGetter
{
  /// <summary>
  /// Based on a state request, returns raw device states per device id
  /// </summary>
  Task<IDictionary<string, IEnumerable<TState>>> GetStates<TState>(StateRequestFilter request) where TState : class, IDeviceState;
}
