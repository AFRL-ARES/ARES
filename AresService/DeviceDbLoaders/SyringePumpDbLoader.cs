using Ares.SyringePump.Ne1000.Messaging;
using AresService.Data;
using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace AresService.DeviceDbLoaders;

public class SyringePumpDbLoader : DeviceDbLoaderBase<ISyringePump, SyringePumpConfig>
{
  public SyringePumpDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<SyringePumpConfig, ISyringePump> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
