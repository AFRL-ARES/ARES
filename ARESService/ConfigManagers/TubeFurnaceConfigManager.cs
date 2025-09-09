using Ares.Core;
using Ares.Core.Device;
using LindbergFurnace;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Config;

namespace AresService.ConfigManagers
{
  public class TubeFurnaceConfigManager : DeviceConfigManagerBase<TubeFurnaceConfig, ITubeFurnace>
  {
    public TubeFurnaceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
      : base(dbContextFactory)
    {
    }
  }
}
