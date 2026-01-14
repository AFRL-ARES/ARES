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
    IDeviceCommandInterpreter<IAresDevice>? interpreter;
    lock(SyncRoot)
    {
      interpreter = this.FirstOrDefault(interp => interp.Device.UniqueId == deviceId);
      if(interpreter is null)
      {
        return false;
      }
    }

    return Remove(interpreter);
  }

  public IDeviceCommandInterpreter<IAresDevice>[] GetSnapshot()
  {
    List<IDeviceCommandInterpreter<IAresDevice>> snapshot;
    lock(SyncRoot)
    {
      snapshot = this.ToList();
    }

    return snapshot.ToArray();
  }

  public TDevice[] GetAresDevices<TDevice>() where TDevice : IAresDevice
  {
    lock(SyncRoot)
    {
      return this.Select(interpreter => interpreter.Device)
        .OfType<TDevice>()
        .ToArray();
    }
  }

  public IAresDevice[] GetAresDevices()
  {
    lock(SyncRoot)
    {
      return this.Select(interpreter => interpreter.Device).ToArray();
    }
  }

  public IDeviceCommandInterpreter<IAresDevice> GetCommandInterpreterByDeviceId(string deviceId)
  {
    lock(SyncRoot)
    {
      return this.First(interpreter => interpreter.Device.UniqueId == deviceId);
    }
  }

  public IAresDevice? GetAresDevice(string deviceId)
  {
    lock(SyncRoot)
    {
      return this.Select(interpreter => interpreter.Device)
        .FirstOrDefault(device => device.UniqueId == deviceId);
    }
  }

  public TDevice? GetAresDevice<TDevice>(string deviceId) where TDevice : IAresDevice
  {
    lock(SyncRoot)
    {
      return this.Select(interpreter => interpreter.Device)
        .OfType<TDevice>()
        .FirstOrDefault(device => device.UniqueId == deviceId);
    }
  }
}
