using System.Threading.Tasks;

namespace ARESCore.DeviceStateLoggers;
public interface IDeviceStateLogger
{
  public string DeviceId { get; }
  public Task Start();
  public Task Stop();
}
