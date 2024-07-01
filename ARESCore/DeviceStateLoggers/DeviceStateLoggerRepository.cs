using System.Collections.Concurrent;

namespace ARESCore.DeviceStateLoggers;
public class DeviceStateLoggerRepository : ConcurrentDictionary<string, IDeviceStateLogger>, IDeviceStateLoggerRepository
{
}
