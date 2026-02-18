using System.Text.Json.Serialization;

namespace UI.Backend.Devices;

public class GridStackWidget
{
  [JsonPropertyName("id")]
  public string? Id { get; set; }
  [JsonPropertyName("x")]
  public int X { get; set; }
  [JsonPropertyName("y")]
  public int Y { get; set; }
  [JsonPropertyName("w")]
  public int W { get; set; }
  [JsonPropertyName("h")]
  public int H { get; set; }
}
