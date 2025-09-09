using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using TC0304;
using Tc0304.Config;

namespace AresService.DeviceDbLoaders;

public class Tc0304DbLoader : DeviceDbLoaderBase<IDataloggerThermometer, Tc0304Config>
{
  public Tc0304DbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<Tc0304Config, IDataloggerThermometer> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
