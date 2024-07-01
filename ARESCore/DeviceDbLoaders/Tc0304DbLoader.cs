using ARESCore;
using ARESCore.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using Tc0304.Config;
using TC0304;

namespace ARESCore.DeviceDbLoaders;

public class Tc0304DbLoader : DeviceDbLoaderBase<IDataloggerThermometer, Tc0304Config>
{
  public Tc0304DbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<Tc0304Config, IDataloggerThermometer> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
