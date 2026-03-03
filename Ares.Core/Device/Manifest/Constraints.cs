using System.Text.Json.Serialization;

namespace Ares.Core.Device.Manifest;

public class Constraints
{
  [JsonPropertyName("regex")]
  public string Regex { get; set; } = string.Empty;

  [JsonPropertyName("min")]
  public double Min { get; set; }

  [JsonPropertyName("max")]
  public double Max { get; set; }

  [JsonPropertyName("validation_hint")]
  public string ValidationHint { get; set; } = string.Empty;
}
