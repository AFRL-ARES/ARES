using ARESCore;
using ARESCore.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using TicStepperController;
using TicStepperController.Config;

namespace ARESCore.DeviceDbLoaders;
public class StepperControllerDbLoader : DeviceDbLoaderBase<IStepperController, StepperControllerConfig>
{
  public StepperControllerDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory,
    IDeviceManager<StepperControllerConfig,
    IStepperController> deviceManager)
    : base(dbContextFactory, deviceManager)
  {
  }
}
