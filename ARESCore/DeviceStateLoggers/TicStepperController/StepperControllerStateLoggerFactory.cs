using ARESCore;
using Microsoft.EntityFrameworkCore;
using TicStepperController;

namespace ARESCore.DeviceStateLoggers.TicStepperController;
public class StepperControllerStateLoggerFactory : IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger>
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public StepperControllerStateLoggerFactory(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IStepperControllerStateLogger Create(IStepperController device)
    => new StepperControllerStateLogger(_dbContextFactory, device);
}
