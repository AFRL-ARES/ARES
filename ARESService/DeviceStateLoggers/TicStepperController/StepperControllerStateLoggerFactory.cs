using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using TicStepperController;

namespace AresService.DeviceStateLoggers.TicStepperController;
public class StepperControllerStateLoggerFactory : IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public StepperControllerStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IStepperControllerStateLogger Create(IStepperController device)
    => new StepperControllerStateLogger(_dbContextFactory, device);
}
