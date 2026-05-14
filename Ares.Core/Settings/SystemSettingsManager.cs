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

  public async Task Initialize()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingGeneralSettings = await context.GeneralSettingsConfigs.FirstOrDefaultAsync();

    if(existingGeneralSettings is null)
    {
      var newGeneralSettingsConfig = new AresGeneralSettingsConfig()
      {
        UniqueId = Guid.NewGuid().ToString(),
        CommandLatency = 0,
        ExperimentRetryLimit = 1,
        RetryCooldown = 0,
        CommandRetryLimit = 1
      };

      context.GeneralSettingsConfigs.Add(newGeneralSettingsConfig);
    }

    var existingErrorHandling = await context.DeviceErrorHandlingConfigs.ToListAsync();
    var allEnumValues = Enum.GetValues<CommandStatusCode>();

    if(existingErrorHandling.Count != allEnumValues.Length)
    {
      var existingCodes = existingErrorHandling.Select(c => c.Code).ToHashSet();
      var missingConfigs = new List<DeviceErrorHandlingConfig>();

      foreach(var code in allEnumValues)
      {
        if(!existingCodes.Contains(code))
        {
          missingConfigs.Add(new DeviceErrorHandlingConfig
          {
            Code = code,
            Handling = ErrorHandling.StopAndCloseout
          });
        }
      }

      if(missingConfigs.Any())
        context.DeviceErrorHandlingConfigs.AddRange(missingConfigs);
    }

    await context.SaveChangesAsync();
  }

  /// <summary>
  /// Updates the databases error handling settings based on the provided new configs.
  /// </summary>
  /// <param name="configs">The configs to be updated in the database.</param>
  /// <returns></returns>
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

  /// <summary>
  /// Gets the latest error handling settings from the database.
  /// </summary>
  /// <returns>An enumerable of device error handling configs representing the latest settings.</returns>
  public async Task<IEnumerable<DeviceErrorHandlingConfig>> GetCurrentErrorHandlingSettings()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var results = await context.DeviceErrorHandlingConfigs.ToListAsync();
    return results;
  }

  /// <summary>
  /// Gets the latest general settings config from the database.
  /// </summary>
  /// <returns>An ARES general settings config containing all the latest settings</returns>
  public async Task<AresGeneralSettingsConfig?> GetAresGeneralSettings()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var config = await context.GeneralSettingsConfigs.FirstOrDefaultAsync();
    return config;
  }

  /// <summary>
  /// Updates the stored settings config in the database to match the provided config settings.
  /// </summary>
  /// <param name="config">A config containing the updated settings.</param>
  /// <returns></returns>
  public async Task UpdateAresGeneralSettings(AresGeneralSettingsConfig config)
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingConfig = await context.GeneralSettingsConfigs.FirstOrDefaultAsync();

    if(existingConfig is not null)
    {
      existingConfig.RetryCooldown = config.RetryCooldown;
      existingConfig.CommandLatency = config.CommandLatency;
      existingConfig.ExperimentRetryLimit = config.ExperimentRetryLimit;
      existingConfig.CommandRetryLimit = config.CommandRetryLimit;

      await context.SaveChangesAsync();
    }
  }
}
