using Ares.Core;
using Ares.Core.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace AresService.ConfigManagers;

public class SyringePumpConfigManager : DeviceConfigManager<SyringePumpConfig, ISyringePump>
{
  public SyringePumpConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
