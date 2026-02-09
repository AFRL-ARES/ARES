using Ares.Messages.DeviceStates;

namespace UI.Features.DeviceStateLogging;

public interface ICombinedDeviceGetter
{
  Task<DevicesDescription[]> GetAvailableDevices();
}
