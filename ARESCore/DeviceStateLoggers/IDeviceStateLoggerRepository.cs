using System.Collections.Generic;

namespace ARESCore.DeviceStateLoggers;
public interface IDeviceStateLoggerRepository : IDictionary<string, IDeviceStateLogger>
{
}
