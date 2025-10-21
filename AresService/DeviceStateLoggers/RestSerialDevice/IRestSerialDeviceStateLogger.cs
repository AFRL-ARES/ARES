using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.RestSerialDevice;

public interface IRestSerialDeviceStateLogger : IDeviceStateLogger, IDisposable
{
}
