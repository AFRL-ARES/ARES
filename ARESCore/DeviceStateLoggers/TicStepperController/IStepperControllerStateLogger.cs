using ARESCore.DeviceStateLoggers;
using System;

namespace ARESCore.DeviceStateLoggers.TicStepperController;
public interface IStepperControllerStateLogger : IDeviceStateLogger, IDisposable
{
}
