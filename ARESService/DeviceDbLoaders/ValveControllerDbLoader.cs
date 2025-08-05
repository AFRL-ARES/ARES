using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using ValveController;
using ValveController.Config;

namespace AresService.DeviceDbLoaders;
public class ValveControllerDbLoader : DeviceDbLoaderBase<IValveController, ValveControllerConfig>
{
  public ValveControllerDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<ValveControllerConfig, IValveController> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
