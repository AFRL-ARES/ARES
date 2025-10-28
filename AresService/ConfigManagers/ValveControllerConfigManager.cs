using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using ValveController;
using ValveController.Config;

namespace AresService.ConfigManagers;
public class ValveControllerConfigManager : DeviceConfigManagerBase<ValveControllerConfig, IValveController>
{
  public ValveControllerConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
