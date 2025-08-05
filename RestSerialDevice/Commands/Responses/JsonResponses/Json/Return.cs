using System.Text.Json.Serialization;

namespace RestSerialDevice.Commands.Responses.JsonResponses.Json;

public class Return
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("dataType")]
  public string DataType { get; set; }

  [JsonPropertyName("unit")]
  public string Unit { get; set; } = string.Empty;
}
