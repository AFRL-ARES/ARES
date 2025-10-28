using System;
using Ares.Core.Device.State.Logging;

namespace AresService.DeviceStateLoggers.TubeFurnace
{
  public interface ITubeFurnaceStateLogger : IDeviceStateLogger, IDisposable
  {
  }
}
