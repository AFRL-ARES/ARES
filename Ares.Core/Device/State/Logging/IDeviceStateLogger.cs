namespace Ares.Core.Device.State.Logging;
public interface IDeviceStateLogger
{
  public string DeviceId { get; }
  public Task Start();
  public Task Stop();
}
