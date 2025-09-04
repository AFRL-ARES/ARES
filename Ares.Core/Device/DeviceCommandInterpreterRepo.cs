using Ares.Core.CoreDevice;
using Ares.Device;

namespace Ares.Core.Device;

public class DeviceCommandInterpreterRepo : SynchronizedCollection<IDeviceCommandInterpreter<IAresDevice>>, IDeviceCommandInterpreterRepo
{
  public DeviceCommandInterpreterRepo()
  {
    var coreDevice = new AresCoreDevice();
    var coreInterpreter = new AresCoreDeviceCommandInterpreter(coreDevice);
    Add(coreInterpreter);
  }

  public bool Remove(string deviceId)
  {
    var interpreter = this.FirstOrDefault(interp => interp.Device.UniqueId == deviceId);
    if (interpreter is null)
    {
      return false;
    }

    return Remove(interpreter);
  }
}
