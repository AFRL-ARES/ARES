using System.Text.Json.Serialization;

namespace RestSerialDevice.Commands.Responses.JsonResponses.Json;

public class Variable
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  [JsonPropertyName("path")]
  public string Path { get; set; } = string.Empty;

  [JsonPropertyName("dataType")]
  public string DataType { get; set; } = string.Empty;

  [JsonPropertyName("uncertainty")]
  public float? Uncertainty { get; set; }

  [JsonPropertyName("unit")]
  public string? Unit { get; set; }

  [JsonPropertyName("readable")]
  public bool Readable { get; set; }

  [JsonPropertyName("writable")]
  public bool Writable { get; set; }
}
