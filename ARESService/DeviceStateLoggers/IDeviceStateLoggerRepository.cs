using System.Collections.Generic;

namespace AresService.DeviceStateLoggers;
public interface IDeviceStateLoggerRepository : IDictionary<string, IDeviceStateLogger>
{
}
