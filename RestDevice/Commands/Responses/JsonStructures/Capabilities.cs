using System.Text.Json.Serialization;

namespace RestDevice.Commands.Responses.JsonStructures;

public class Capabilities
{
  [JsonPropertyName("variables")]
  public List<Variable> Variables { get; set; } = new List<Variable>();

  [JsonPropertyName("functions")]
  public List<Function> Functions { get; set; } = new List<Function>();
}
