using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using TicStepperController;
using TicStepperController.Config;

namespace ARESCore.ConfigManagers;
public class StepperControllerConfigManager : DeviceConfigManagerBase<StepperControllerConfig, IStepperController>
{
  public StepperControllerConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
    : base(dbContextFactory)
  {
  }
}
