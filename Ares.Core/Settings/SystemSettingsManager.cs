using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Settings;

public class SystemSettingsManager : ISystemSettingsManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public SystemSettingsManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;    
  }

  public async Task UpdateErrorHandlingSettings(List<DeviceErrorHandlingConfig> configs)
  {
    using var context = _dbContextFactory.CreateDbContext();
    foreach(var config in configs)
    {
      var matchingConfig = context.DeviceErrorHandlingConfigs.FirstOrDefault(c => c.Code == config.Code);

      if(matchingConfig is null)
        context.DeviceErrorHandlingConfigs.Add(config);

      else
        matchingConfig.Handling = config.Handling;
    }

    await context.SaveChangesAsync();
  }

  public async Task<IEnumerable<DeviceErrorHandlingConfig>> GetCurrentErrorHandlingSettings()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var results = await context.DeviceErrorHandlingConfigs.ToListAsync();
    return results;
  }
}
