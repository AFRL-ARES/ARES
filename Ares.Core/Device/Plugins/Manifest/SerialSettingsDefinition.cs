using System.Text.Json.Serialization;

namespace Ares.Core.Device.Plugins.Manifest;

/// <summary>
/// Defines the serial connection constraints and requirements for the device.
/// </summary>
public class SerialSettingsDefinition
{
  [JsonPropertyName("requires_unit_id")]
  public bool RequiresUnitId { get; set; }

  [JsonPropertyName("unit_id_regex")]
  public string UnitIdRegex { get; set; } = string.Empty;

  [JsonPropertyName("unit_id_validation_hint")]
  public string UnitIdValidationHint { get; set; } = string.Empty;

  [JsonPropertyName("variable_baud_rate")]
  public bool VariableBaudRate { get; set; }

  [JsonPropertyName("allowed_baud_rates")]
  public List<int> AllowedBaudRates { get; set; } = new();

  [JsonPropertyName("default_baud_rate")]
  public int DefaultBaudRate { get; set; }
}