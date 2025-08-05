using Ares.Device;

namespace Ares.Core.Device
{
  public interface IDeviceCommandInterpreterRepo : ICollection<IDeviceCommandInterpreter<IAresDevice>>
  {
  }
}
