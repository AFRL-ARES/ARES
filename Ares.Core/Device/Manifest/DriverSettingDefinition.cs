using System.Text.Json.Serialization;

namespace Ares.Core.Device.Manifest;

public class DriverSettingDefinition
{
  [JsonPropertyName("key")]
  public string Key { get; set; } = string.Empty;

  [JsonPropertyName("display_name")]
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// The data type of the setting (e.g., "string", "boolean", "int").
  /// </summary>
  [JsonPropertyName("type")]
  public string Type { get; set; } = "string";

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// The default value for this setting. 
  /// Note: Using 'object' allows this to hold booleans, strings, or numbers 
  /// depending on the 'Type' property.
  /// </summary>
  [JsonPropertyName("default")]
  public object? Default { get; set; }
}
