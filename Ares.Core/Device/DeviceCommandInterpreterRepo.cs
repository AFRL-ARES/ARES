using Ares.Device;
using CoreDevice;
using System.Collections.ObjectModel;

namespace Ares.Core.Device;

public class DeviceCommandInterpreterRepo : Collection<IDeviceCommandInterpreter<IAresDevice>>, IDeviceCommandInterpreterRepo
{
  public DeviceCommandInterpreterRepo()
  {
    var coreDevice = new AresCoreDevice();
    var coreInterpreter = new AresCoreDeviceCommandInterpreter(coreDevice);
    Add(coreInterpreter);
  }
}
