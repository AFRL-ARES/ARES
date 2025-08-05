using AresService.DeviceManagers;
using LindbergFurnace;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Config;

namespace AresService.DeviceDbLoaders
{
  public class TubeFurnaceDbLoader : DeviceDbLoaderBase<ITubeFurnace, TubeFurnaceConfig>
  {
    public TubeFurnaceDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<TubeFurnaceConfig, ITubeFurnace> deviceManager) : base(dbContextFactory, deviceManager)
    {
    }
  }
}
