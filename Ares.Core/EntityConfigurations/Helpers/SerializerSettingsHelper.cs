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
}
