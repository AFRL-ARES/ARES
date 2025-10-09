using AresService.Data;
using AresService.DeviceManagers;
using HerkulexDRS;
using HerkulexDRS.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;
public class ServoDbLoader : DeviceDbLoaderBase<IServo, ServoConfig>
{
  public ServoDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<ServoConfig, IServo> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
