using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.RestDevice;

public interface IRestDeviceStateLogger : IDeviceStateLogger, IDisposable
{
}
