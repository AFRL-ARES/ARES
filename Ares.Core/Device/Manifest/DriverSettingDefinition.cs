using System.Text.Json.Serialization;

namespace Ares.Core.Device.Manifest;

public class DriverSettingDefinition
{
  /// <summary>
  /// The name to be associated with the setting.
  /// </summary>
  [JsonPropertyName("key")]
  public string Key { get; set; } = string.Empty;

  /// <summary>
  /// A friendly display name for the UI to use when referring to this setting.
  /// </summary>
  [JsonPropertyName("display_name")]
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// The data type of the setting (e.g., "string", "boolean", "int").
  /// </summary>
  [JsonPropertyName("type")]
  public string Type { get; set; } = "string";

  /// <summary>
  /// A brief description of the setting.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// The constraints applied to the driver setting.
  /// Can be supplied as a min + max OR a regex.
  /// </summary>
  [JsonPropertyName("constraints")]
  public Constraints Constraints { get; set; } = new();
}
