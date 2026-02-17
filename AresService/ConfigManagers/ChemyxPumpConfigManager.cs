using Ares.Core;
using Ares.Core.Device;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class ChemyxPumpConfigManager : DeviceConfigManager<ChemyxPumpConfig, IChemyxPump>
{
  public ChemyxPumpConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
