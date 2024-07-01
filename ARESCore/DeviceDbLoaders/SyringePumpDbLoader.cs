using Ares.SyringePump.Ne1000.Messaging;
using ARESCore;
using ARESCore.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace ARESCore.DeviceDbLoaders;

public class SyringePumpDbLoader : DeviceDbLoaderBase<ISyringePump, SyringePumpConfig>
{
  public SyringePumpDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<SyringePumpConfig, ISyringePump> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
