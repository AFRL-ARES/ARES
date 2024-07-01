using Ares.Core.Device;
using Ares.Core;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Config;
using LindbergFurnace;

namespace ARESCore.ConfigManagers
{
  public class TubeFurnaceConfigManager : DeviceConfigManagerBase<TubeFurnaceConfig, ITubeFurnace>
  {
    public TubeFurnaceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
      : base(dbContextFactory)
    {
    }
  }
}
