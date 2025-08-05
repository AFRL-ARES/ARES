using System;

namespace AresService.DeviceStateLoggers.TicStepperController;

public interface IStepperControllerStateLogger : IDeviceStateLogger, IDisposable
{
}
