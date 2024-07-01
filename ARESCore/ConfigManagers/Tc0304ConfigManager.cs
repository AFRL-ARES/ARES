using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using TC0304;
using Tc0304.Config;

namespace ARESCore.ConfigManagers;

public class Tc0304ConfigManager : DeviceConfigManagerBase<Tc0304Config, IDataloggerThermometer>
{
  public Tc0304ConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
