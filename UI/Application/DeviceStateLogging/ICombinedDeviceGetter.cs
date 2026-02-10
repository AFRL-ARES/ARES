using Ares.Messages.DeviceStates;

namespace UI.Application.DeviceStateLogging;

public interface ICombinedDeviceGetter
{
  Task<DevicesDescription[]> GetAvailableDevices();
}
