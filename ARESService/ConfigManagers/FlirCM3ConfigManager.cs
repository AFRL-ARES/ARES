using Ares.Core;
using Ares.Core.Device;
using FlirCM3;
using FlirCM3.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class FlirCM3ConfigManager : DeviceConfigManagerBase<FlirCM3Config, IFlirCM3Camera>
{
  public FlirCM3ConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
