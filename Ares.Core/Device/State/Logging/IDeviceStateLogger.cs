using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Logging;
public interface IDeviceStateLogger
{
  public string DeviceId { get; }
  public Task Start(DeviceLoggingSettings? settings = null);
  public Task Stop();
}
