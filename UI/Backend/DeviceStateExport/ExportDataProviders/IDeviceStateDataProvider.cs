using Ares.Messages.DeviceStates;

namespace UI.Backend.DeviceStateExport.ExportDataProviders;

/// <summary>
/// This interface defines how to get the state data for multiple devices of the same type
/// </summary>
public interface IDeviceStateDataProvider
{
  Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(StateRequest request);
}
