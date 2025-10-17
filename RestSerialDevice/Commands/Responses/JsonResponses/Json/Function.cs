using System.Text.Json.Serialization;

namespace RestSerialDevice.Commands.Responses.JsonResponses.Json;

public class Function
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("path")]
  public string Path { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  [JsonPropertyName("parameters")]
  public List<Parameter> Parameters { get; set; } = [];

  [JsonPropertyName("returns")]
  public List<Return> Returns { get; set; } = [];
}
