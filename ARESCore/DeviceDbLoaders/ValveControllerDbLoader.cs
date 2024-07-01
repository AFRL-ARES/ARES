using ARESCore;
using ARESCore.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using ValveController;
using ValveController.Config;

namespace ARESCore.DeviceDbLoaders;
public class ValveControllerDbLoader : DeviceDbLoaderBase<IValveController, ValveControllerConfig>
{
  public ValveControllerDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<ValveControllerConfig, IValveController> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
