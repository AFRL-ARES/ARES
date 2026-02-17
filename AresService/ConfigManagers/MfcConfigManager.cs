using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class MfcConfigManager : DeviceConfigManager<MfcConfig, IMassFlowController>
{
  public MfcConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
