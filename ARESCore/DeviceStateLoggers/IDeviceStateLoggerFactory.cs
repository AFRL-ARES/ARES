using Ares.Device;

namespace ARESCore.DeviceStateLoggers;
public interface IDeviceStateLoggerFactory<in TDevice, out TStateLogger>
  where TDevice : IAresDevice
  where TStateLogger : IDeviceStateLogger
{
  TStateLogger Create(TDevice device);
}
