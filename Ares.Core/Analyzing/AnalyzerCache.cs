using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Analyzing;
internal class AnalyzerCache(IDbContextFactory<CoreDatabaseContext> _dbContextFactory)
: IAnalyzerCache
{
  public async Task<AresStruct?> GetCachedAnalyzerSettings(string analyzerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = await ctx.AnalyzerSettings.FirstOrDefaultAsync(settings => settings.AnalyzerId == analyzerId);
    return settings?.Settings;
  }

  public async Task<AnalyzerInfo?> GetCachedAnalyzerInfo(string analyzerId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var info = await ctx.AnalyzerInfos.FirstOrDefaultAsync(info => info.UniqueId == analyzerId);
    return info;
  }

  public async Task CacheAnalyzerSettings(RemoteAnalyzer analyzer)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var settings = analyzer.Settings;
    var existingSettings = await ctx.AnalyzerSettings
      .FirstOrDefaultAsync(setting => setting.AnalyzerId == analyzer.UniqueId);

    if(existingSettings is not null)
    {
      existingSettings.Settings = settings;
      await ctx.SaveChangesAsync();
    }
    else
    {
      var newSettings = new AnalyzerSettings
      {
        AnalyzerId = analyzer.UniqueId,
        Settings = settings
      };
      ctx.AnalyzerSettings.Add(newSettings);
      await ctx.SaveChangesAsync();
    }
  }

  public async Task CacheAnalyzerInfo(RemoteAnalyzer analyzer)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var currentInfo = await AnalyzerToAnalyzerInfo(analyzer);
    var existingInfoInDb = await ctx.AnalyzerInfos.FirstOrDefaultAsync(info => info.UniqueId == analyzer.UniqueId);

    if(existingInfoInDb is not null)
    {
      existingInfoInDb.Name = analyzer.Name;
      existingInfoInDb.Type = analyzer.Type;
      existingInfoInDb.Description = analyzer.Description;
      existingInfoInDb.Url = analyzer.Address.ToString();
      existingInfoInDb.Version = analyzer.Version;
      existingInfoInDb.Capabilities = currentInfo.Capabilities;

      await ctx.SaveChangesAsync();
    }
    else
    {
      ctx.AnalyzerInfos.Add(currentInfo);
      await ctx.SaveChangesAsync();
    }
  }

  private static async Task<AnalyzerInfo> AnalyzerToAnalyzerInfo(RemoteAnalyzer analyzer)
  {
    var capabilities = await analyzer.GetCapabilities();

    return new AnalyzerInfo
    {
      Name = analyzer.Name,
      Type = analyzer.Type,
      Description = analyzer.Description,
      UniqueId = analyzer.UniqueId,
      Url = analyzer.Address.ToString(),
      Version = analyzer.Version,
      Capabilities = capabilities
    };
  }
}
