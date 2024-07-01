using ARESCore;
using ARESCore.DeviceManagers;
using LindbergFurnace;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Config;

namespace ARESCore.DeviceDbLoaders
{
  public class TubeFurnaceDbLoader : DeviceDbLoaderBase<ITubeFurnace, TubeFurnaceConfig>
  {
    public TubeFurnaceDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<TubeFurnaceConfig, ITubeFurnace> deviceManager) : base(dbContextFactory, deviceManager)
    {
    }
  }
}
