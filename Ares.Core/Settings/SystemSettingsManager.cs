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

  /// <summary>
  /// Based on the provided status code, returns the current setting for that codes error handling procedure.
  /// </summary>
  /// <param name="code">The status code the handling protocol is being requested for.</param>
  /// <returns>The error handling protocol assigned to that error code.</returns>
  public async Task<ErrorHandling> GetErrorHandlingByStatusCode(CommandStatusCode code)
  {
    try
    {
      using var context = _dbContextFactory.CreateDbContext();
      var matchingSetting = await context.DeviceErrorHandlingConfigs.FirstAsync(c => c.Code == code);
      return matchingSetting.Handling;
    }

    catch(Exception)
    {
      return ErrorHandling.UnknownHandling;
    }
  }

  public async Task<IEnumerable<DeviceErrorHandlingConfig>> GetCurrentErrorHandlingSettings()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var results = await context.DeviceErrorHandlingConfigs.ToListAsync();
    return results;
  }
}
