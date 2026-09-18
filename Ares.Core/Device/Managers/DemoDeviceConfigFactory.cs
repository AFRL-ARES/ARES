using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Providers;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Device.Managers;

/// <summary>
/// Helper factory for constructing demo-mode <see cref="DeviceConfig"/> instances.
/// Demo configs use static IDs from <see cref="DemoIds"/> and resolve driver IDs
/// from the loaded plugin drivers.
/// </summary>
internal static class DemoDeviceConfigFactory
{
  public static IReadOnlyList<DeviceConfig> CreateDemoConfigs(IDeviceDriverProvider driverProvider)
  {
    var drivers = driverProvider.GetAllDeviceDrivers();
    var configs = new List<DeviceConfig>();

    var mfcConfig = TryCreateMfcConfig(drivers);
    if (mfcConfig is not null)
      configs.Add(mfcConfig);

    var syringeConfig = TryCreateSyringePumpConfig(drivers);
    if (syringeConfig is not null)
      configs.Add(syringeConfig);

    return configs;
  }

  private static DeviceConfig? TryCreateMfcConfig(IReadOnlyCollection<DeviceDriver> drivers)
  {
    var mfcDriver = drivers.FirstOrDefault(d =>
      string.Equals(d.Manifest.DeviceTypeName, "Alicat Mass Flow Controller", StringComparison.OrdinalIgnoreCase));

    if (mfcDriver is null)
      return null;

    var settings = new Dictionary<string, AresValue>
    {
      { "HasValve", AresValueHelper.CreateBool(true) },
      { "IsBasis", AresValueHelper.CreateBool(true) }
    };

    var mfcConfig = new DeviceConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceId = DemoIds.DemoMFCId,
      DeviceName = DemoIds.DemoMFCName,
      DriverId = mfcDriver.UniqueId,
      IsSimulated = true,
      DeviceSettings = new(),
      SerialInfo = mfcDriver.Manifest.SerialSettings is null
        ? null
        : new SerialConnection
        {
          PortName = "SimCOM1",
          Protocol = mfcDriver.Manifest.SerialSettings.DefaultProtocol,
          SerialId = "A"
        }
    };

    mfcConfig.DeviceSettings.AddBool("IsBasis", false);
    mfcConfig.DeviceSettings.AddBool("HasValve", true);

    return mfcConfig;
  }

  private static DeviceConfig? TryCreateSyringePumpConfig(IReadOnlyCollection<DeviceDriver> drivers)
  {
    var syringeDriver = drivers.FirstOrDefault(d =>
      string.Equals(d.Manifest.DeviceTypeName, "Chemyx Syringe Pump", StringComparison.OrdinalIgnoreCase));

    if(syringeDriver is null)
      return null;

    var syringeConfig = new DeviceConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceId = DemoIds.DemoSyringePumpId,
      DeviceName = DemoIds.DemoSyringePumpName,
      DriverId = syringeDriver.UniqueId,
      IsSimulated = true,
      DeviceSettings = new(),
      SerialInfo = syringeDriver.Manifest.SerialSettings is null
        ? null
        : new SerialConnection
        {
          PortName = "SimCOM2",
          Protocol = syringeDriver.Manifest.SerialSettings.DefaultProtocol
        }
    };

    syringeConfig.DeviceSettings.AddBool("DualPump", true);

    return syringeConfig;
  }
}
