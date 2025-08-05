using System;

namespace Ares.Device;
public interface IAresDeviceConnection : IDisposable
{
  string? Name { get; }
  bool IsOpen { get; }
}
