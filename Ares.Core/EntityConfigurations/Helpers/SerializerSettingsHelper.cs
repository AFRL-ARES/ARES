using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class SerializerSettingsHelper
{
  public static JsonSerializerOptions CreateCustomSerializationSettings()
  {
    var options = new JsonSerializerOptions();
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.AddProtobufSupport();
    return options;
  }

  public static string DetermineColumnType()
  {
    var provider = DatabaseRuntimeEnvironment.DatabaseProvider;

    if(provider is null)
      return "TEXT";

    if(provider.Contains("Postgres", StringComparison.CurrentCultureIgnoreCase))
      return "jsonb";

    if(provider.Contains("Sqlite", StringComparison.CurrentCultureIgnoreCase))
      return "TEXT";

    if(provider.Contains("SqlServer", StringComparison.CurrentCultureIgnoreCase))
      return "nvarchar(max)";

    else
      return "TEXT";
  }
}
