using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using TicStepperController;
using TicStepperController.Config;

namespace AresService.ConfigManagers;
public class StepperControllerConfigManager : DeviceConfigManager<StepperControllerConfig, IStepperController>
{
  public StepperControllerConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
    : base(dbContextFactory)
  {
  }
}
