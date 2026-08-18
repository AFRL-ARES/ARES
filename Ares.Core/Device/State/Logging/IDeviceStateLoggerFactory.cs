using Ares.Device;

namespace Ares.Core.Device.State.Logging;

public interface IDeviceStateLoggerFactory
{
  IDeviceStateLogger Create(IAresDevice device);
}
