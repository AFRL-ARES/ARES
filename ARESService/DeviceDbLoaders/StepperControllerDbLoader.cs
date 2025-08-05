using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using TicStepperController;
using TicStepperController.Config;

namespace AresService.DeviceDbLoaders;
public class StepperControllerDbLoader : DeviceDbLoaderBase<IStepperController, StepperControllerConfig>
{
  public StepperControllerDbLoader(IDbContextFactory<AresDbContext> dbContextFactory,
    IDeviceManager<StepperControllerConfig,
    IStepperController> deviceManager)
    : base(dbContextFactory, deviceManager)
  {
  }
}
