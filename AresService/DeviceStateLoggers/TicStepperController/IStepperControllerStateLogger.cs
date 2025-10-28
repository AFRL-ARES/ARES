using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.TicStepperController;

public interface IStepperControllerStateLogger : IDeviceStateLogger, IDisposable
{
}
