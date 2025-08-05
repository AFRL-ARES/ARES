using System.Collections.Generic;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates;

namespace AresService.DeviceStateExport.ExportDataProviders;

/// <summary>
/// This interface defines how to get the state data for multiple devices of the same type
/// </summary>
public interface IDeviceStateDataProvider
{
  Task<IEnumerable<SingleDeviceStateExportData>> GetExportData(StateRequestFilter request);
}
