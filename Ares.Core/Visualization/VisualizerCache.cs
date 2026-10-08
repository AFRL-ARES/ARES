using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Visualization;

internal class VisualizerCache(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : IVisualizerCache
{
  public async Task<AresStruct?> GetCachedVisualizerSettings(string visualizerId)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var settings = await context.VisualizerSettings.FirstOrDefaultAsync(settings => settings.VisualizerId == visualizerId);
    return settings?.Settings;
  }

  public async Task<VisualizerInfo?> GetCachedVisualizerInfo(string visualizerId)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    return await context.VisualizerInfos.FirstOrDefaultAsync(info => info.UniqueId == visualizerId);
  }

  public async Task CacheVisualizerSettings(RemoteVisualizer visualizer)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var existingSettings = await context.VisualizerSettings
      .FirstOrDefaultAsync(settings => settings.VisualizerId == visualizer.UniqueId);

    if(existingSettings is not null)
      existingSettings.Settings = visualizer.Settings;
    
    else
    {
      context.VisualizerSettings.Add(new VisualizerSettings
      {
        VisualizerId = visualizer.UniqueId,
        Settings = visualizer.Settings
      });
    }

    await context.SaveChangesAsync();
  }

  public async Task CacheVisualizerInfo(RemoteVisualizer visualizer)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var existingInfo = await context.VisualizerInfos.FirstOrDefaultAsync(info => info.UniqueId == visualizer.UniqueId);

    if(existingInfo is not null)
    {
      existingInfo.Name = visualizer.Name;
      existingInfo.Type = visualizer.Type;
      existingInfo.Description = visualizer.Description;
      existingInfo.Url = visualizer.Address.ToString();
      existingInfo.Version = visualizer.Version;
    }
    else
    {
      context.VisualizerInfos.Add(new VisualizerInfo
      {
        UniqueId = visualizer.UniqueId,
        Name = visualizer.Name,
        Type = visualizer.Type,
        Description = visualizer.Description,
        Url = visualizer.Address.ToString(),
        Version = visualizer.Version
      });
    }

    await context.SaveChangesAsync();
  }
}