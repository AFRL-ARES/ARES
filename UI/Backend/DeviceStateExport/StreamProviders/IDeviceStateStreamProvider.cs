using Ares.Messages.DeviceStates;

namespace UI.Backend.DeviceStateExport.StreamProviders;

public interface IDeviceStateStreamProvider
{
  Task<IEnumerable<DeviceStateStream>> GetStream(StateRequest request);
}
