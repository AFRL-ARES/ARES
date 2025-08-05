using System.Collections.Concurrent;

namespace AresService.DeviceStateLoggers;
public class DeviceStateLoggerRepository : ConcurrentDictionary<string, IDeviceStateLogger>, IDeviceStateLoggerRepository
{
}
