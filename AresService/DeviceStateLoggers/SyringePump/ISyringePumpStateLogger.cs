using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.SyringePump;

public interface ISyringePumpStateLogger : IDeviceStateLogger, IDisposable
{
}
