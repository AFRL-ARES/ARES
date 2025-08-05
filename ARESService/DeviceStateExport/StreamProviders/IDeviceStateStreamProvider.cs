using System.Collections.Generic;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;

namespace AresService.DeviceStateExport.StreamProviders;

public interface IDeviceStateStreamProvider
{
  Task<IEnumerable<DeviceStateStream>> GetStream(StateRequestFilter request);
}
