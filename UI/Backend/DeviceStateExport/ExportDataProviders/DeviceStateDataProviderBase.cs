using Ares.Messages.DeviceStates;
using ARESMessaging.DeviceStateLogging;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders;

public abstract class DeviceStateDataProviderBase<TState> : IDeviceStateDataProvider
  where TState : IDeviceState
{
  readonly IDeviceStateGetter<TState> _stateGetter;
  public DeviceStateDataProviderBase(IDeviceStateGetter<TState> stateGetter)
  {
    _stateGetter = stateGetter;
  }

  public async Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(StateRequest request)
  {
    var states = await _stateGetter.GetStates(request);
    var exportData = new List<SingleDeviceStateExportData>();
    foreach (var group in states)
    {
      var lines = GetExportLines(group.Key, group.Value).OrderBy(l => l.Timestamp).ToArray();
      var data = new SingleDeviceStateExportData(group.Key, lines);
      exportData.Add(data);
    }

    return exportData;
  }

  protected abstract IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<TState> deviceStates);
}
