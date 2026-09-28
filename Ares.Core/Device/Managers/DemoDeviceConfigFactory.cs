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

    var catalystConfig = TryCreateCatalystMfcConfig(drivers);
    if(catalystConfig is not null)
      configs.Add(catalystConfig);

    var nitrogenConfig = TryCreateNitrogenMfcConfig(drivers);
    if(nitrogenConfig is not null)
      configs.Add(nitrogenConfig);

    var syringeConfig = TryCreateSyringePumpConfig(drivers);
    if(syringeConfig is not null)
      configs.Add(syringeConfig);

    var furnaceConfig = TryCreateTubeFurnaceConfing(drivers);
    if(furnaceConfig is not null)
      configs.Add(furnaceConfig);

    return configs;
  }

  private static DeviceConfig? TryCreateCatalystMfcConfig(IReadOnlyCollection<DeviceDriver> drivers)
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
      DeviceId = DemoIds.DemoCatalystMFCUniqueID,
      DeviceName = DemoIds.DemoCatalystMFCName,
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

  private static DeviceConfig? TryCreateNitrogenMfcConfig(IReadOnlyCollection<DeviceDriver> drivers)
  {
    var mfcDriver = drivers.FirstOrDefault(d =>
      string.Equals(d.Manifest.DeviceTypeName, "Alicat Mass Flow Controller", StringComparison.OrdinalIgnoreCase));

    if(mfcDriver is null)
      return null;

    var settings = new Dictionary<string, AresValue>
    {
      { "HasValve", AresValueHelper.CreateBool(true) },
      { "IsBasis", AresValueHelper.CreateBool(true) }
    };

    var mfcConfig = new DeviceConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceId = DemoIds.DemoNitrogenMFCUniqueID,
      DeviceName = DemoIds.DemoNitrogenMFCName,
      DriverId = mfcDriver.UniqueId,
      IsSimulated = true,
      DeviceSettings = new(),
      SerialInfo = mfcDriver.Manifest.SerialSettings is null
        ? null
        : new SerialConnection
        {
          PortName = "SimCOM1",
          Protocol = mfcDriver.Manifest.SerialSettings.DefaultProtocol,
          SerialId = "B"
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

  private static DeviceConfig? TryCreateTubeFurnaceConfing(IReadOnlyCollection<DeviceDriver> drivers)
  {
    var furnaceDriver = drivers.FirstOrDefault(d => string.Equals(d.Manifest.DeviceTypeName, "Lindberg Tube Furnace", StringComparison.OrdinalIgnoreCase));

    if(furnaceDriver is null)
      return null;

    var furnaceConfig = new DeviceConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceId = DemoIds.DemoTubeFurnaceId,
      DeviceName = DemoIds.DemoTubeFurnaceName,
      DriverId = furnaceDriver.UniqueId,
      IsSimulated = true,
      DeviceSettings = new(),
      SerialInfo = furnaceDriver.Manifest.SerialSettings is null
      ? null
      : new SerialConnection
      {
        PortName = "SimCOM3",
        Protocol = furnaceDriver.Manifest.SerialSettings.DefaultProtocol
      }
    };

    return furnaceConfig;
  }
}
