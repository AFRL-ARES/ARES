using Ares.Core.CoreDevice;
using Ares.Device;
using System.Collections.Concurrent;

namespace Ares.Core.Device;

public class DeviceCommandInterpreterRepo : SynchronizedCollection<IDeviceCommandInterpreter<IAresDevice>>, IDeviceCommandInterpreterRepo
{
  private ConcurrentBag<IDeviceCommandInterpreter<IAresDevice>> _bag = new();

  public DeviceCommandInterpreterRepo()
  {
    var coreDevice = new AresCoreDevice();
    var coreInterpreter = new AresCoreDeviceCommandInterpreter(coreDevice);
    Add(coreInterpreter);
  }
}
