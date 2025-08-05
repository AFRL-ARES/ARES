using System.Text.Json.Serialization;

namespace RestDevice.Commands.Responses.JsonStructures;

public class Return
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("dataType")]
  public string DataType { get; set; } = string.Empty;

  [JsonPropertyName("unit")]
  public string Unit { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;
}
