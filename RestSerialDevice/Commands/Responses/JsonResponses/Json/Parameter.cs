using System.Text.Json.Serialization;

namespace RestSerialDevice.Commands.Responses.JsonResponses.Json;

public class Parameter
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("dataType")]
  public string DataType { get; set; } = string.Empty;

  [JsonPropertyName("min")]
  public int? Minimum { get; set; }

  [JsonPropertyName("max")]
  public int? Maximum { get; set; }

  [JsonPropertyName("unit")]
  public string? unit { get; set; }
}
