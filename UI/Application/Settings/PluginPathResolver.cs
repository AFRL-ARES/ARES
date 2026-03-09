namespace UI.Application.Settings;

public static class PluginPathResolver
{
  public static string Resolve(AppSettings? settings)
  {
    if(!string.IsNullOrWhiteSpace(settings?.PluginsPath))
    {
      return Path.GetFullPath(settings.PluginsPath);
    }

    var candidates = new[]
    {
      Path.Combine(Environment.CurrentDirectory, "Plugins"),
      Path.Combine(Environment.CurrentDirectory, "..", "Plugins"),
      Path.Combine(AppContext.BaseDirectory, "Plugins"),
      Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Plugins")
    }
    .Select(Path.GetFullPath)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

    return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
  }
}
