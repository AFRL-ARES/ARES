using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using TicStepperController;

namespace AresService.DeviceStateLoggers.TicStepperController;
public class StepperControllerStateLoggerFactory : DeviceStateLoggerFactory<IStepperController>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public StepperControllerStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(IStepperController device)
    => new StepperControllerStateLogger(_dbContextFactory, device);
}
