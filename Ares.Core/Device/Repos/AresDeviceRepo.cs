using Ares.Core.CoreDevice;
using Ares.Device;

namespace Ares.Core.Device.Repos;

public class AresDeviceRepo : SynchronizedCollection<IAresDevice>, IAresDeviceRepo
{
  public AresDeviceRepo()
  {
    var coreDevice = new AresCoreDevice();
    Add(coreDevice);
  }

  public TDevice[] GetAresDevices<TDevice>() where TDevice : IAresDevice
  {
    lock(SyncRoot)
    {
      return this.Select(device => device)
        .OfType<TDevice>()
        .ToArray();
    }
  }

  public IAresDevice? GetAresDevice(string deviceId)
  {
    lock(SyncRoot)
    {
      return this.Select(device => device)
        .FirstOrDefault(device => device.UniqueId == deviceId);
    }
  }

  public TDevice? GetAresDevice<TDevice>(string deviceId) where TDevice : IAresDevice
  {
    lock(SyncRoot)
    {
      return this.Select(device => device)
        .OfType<TDevice>()
        .FirstOrDefault(device => device.UniqueId == deviceId);
    }
  }

  public IAresDevice[] GetSnapshot()
  {
    List<IAresDevice> snapshot;
    lock(SyncRoot)
    {
      snapshot = this.ToList();
    }

    return snapshot.ToArray();
  }

  public bool Remove(string deviceId)
  {
    IAresDevice? device;
    lock(SyncRoot)
    {
      device = this.FirstOrDefault(device => device.UniqueId == deviceId);
      if(device is null)
        return false;
    }

    return Remove(device);
  }
}
