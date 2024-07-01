using Ares.Core;
using Ares.Core.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace ARESCore.ConfigManagers;

public class SyringePumpConfigManager : DeviceConfigManagerBase<SyringePumpConfig, ISyringePump>
{
  public SyringePumpConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
