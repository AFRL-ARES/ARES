using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.LaserChiller;

public interface ILaserChillerStateLogger : IDeviceStateLogger, IDisposable
{
}
