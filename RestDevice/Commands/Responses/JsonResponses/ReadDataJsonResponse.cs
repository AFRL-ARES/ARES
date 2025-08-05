using System.Text.Json.Serialization;

namespace RestDevice.Commands.Responses.JsonResponses;

public class ReadDataJsonResponse
{
  [JsonPropertyName("variables")]
  public Dictionary<string, object> Variables { get; set; } = new();

  [JsonPropertyName("id")]
  public string? Id { get; set; }

  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("hardware")]
  public string? Hardware { get; set; }

  [JsonPropertyName("connected")]
  public bool Connected { get; set; }
}
