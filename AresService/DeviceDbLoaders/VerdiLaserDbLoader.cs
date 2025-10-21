using AresService.Data;
using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using VerdiV6.Config;
using VerdiV6Laser;

namespace AresService.DeviceDbLoaders;

public class VerdiLaserDbLoader : DeviceDbLoaderBase<IVerdiV6Laser, VerdiConfig>
{
  public VerdiLaserDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<VerdiConfig, IVerdiV6Laser> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
