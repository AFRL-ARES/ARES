using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using AresService.DeviceStateExport.StateGetters;
using AresMessaging.DeviceStateLogging;

namespace AresService.DeviceStateExport.ExportDataProviders;

public abstract class DeviceStateDataProviderBase<TState> : IDeviceStateDataProvider where TState : class,  IDeviceState
{
  readonly IDeviceStateGetter _stateGetter;
  public DeviceStateDataProviderBase(IDeviceStateGetter stateGetter)
  { _stateGetter = stateGetter; }

  public async Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(StateRequestFilter filter)
  {
    var states = await _stateGetter.GetStates<TState>(filter);
    var exportData = new List<SingleDeviceStateExportData>();
    foreach(var group in states)
    {
      var lines = GetExportLines(group.Key, group.Value).OrderBy(l => l.Timestamp).ToArray();
      var data = new SingleDeviceStateExportData(group.Key, lines);
      exportData.Add(data);
    }

    return exportData;
  }

  protected abstract IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<TState> deviceStates);
}
