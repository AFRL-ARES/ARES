using Ares.Core;
using Ares.Core.Device;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class ChemyxPumpConfigManager : DeviceConfigManagerBase<ChemyxPumpConfig, IChemyxPump>
{
  public ChemyxPumpConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {
  }
}
