using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using VerdiV6.Config;
using VerdiV6Laser;

namespace AresService.ConfigManagers
{
  public class VerdiLaserConfigManager : DeviceConfigManager<VerdiConfig, IVerdiV6Laser>
  {
    public VerdiLaserConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
    {

    }
  }
}
