using Ares.Device;

namespace AresService.DeviceStateLoggers;
public interface IDeviceStateLoggerFactory<in TDevice, out TStateLogger>
  where TDevice : IAresDevice
  where TStateLogger : IDeviceStateLogger
{
  TStateLogger Create(TDevice device);
}
