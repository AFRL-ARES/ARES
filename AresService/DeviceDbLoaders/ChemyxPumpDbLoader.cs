using AresService.Data;
using AresService.DeviceManagers;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;

public class ChemyxPumpDbLoader : DeviceDbLoaderBase<IChemyxPump, ChemyxPumpConfig>
{
  public ChemyxPumpDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<ChemyxPumpConfig, IChemyxPump> deviceManager) : base(dbContextFactory, deviceManager)
  {    
  }
}
