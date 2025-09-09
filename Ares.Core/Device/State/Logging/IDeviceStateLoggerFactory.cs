using Ares.Device;

namespace Ares.Core.Device.State.Logging;
public interface IDeviceStateLoggerFactory<in TDevice, out TStateLogger>
  where TDevice : IAresDevice
  where TStateLogger : IDeviceStateLogger
{
  TStateLogger Create(TDevice device);
}
