using Ares.Core;
using Ares.Core.Device;
using HerkulexDRS;
using HerkulexDRS.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;
public class ServoConfigManager : DeviceConfigManagerBase<ServoConfig, IServo>
{
  public ServoConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
