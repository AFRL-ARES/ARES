using System.Text.Json.Serialization;

namespace GenericSerialDevice.Commands.Responses.JsonResponses;

public class GetDeviceInfoJsonResponse
{
  [JsonPropertyName("id")]
  public string DeviceId { get; set; } = string.Empty;

  [JsonPropertyName("name")]
  public string DeviceName { get; set; } = string.Empty;

  [JsonPropertyName("hardware")]
  public string Hardware { get; set; } = string.Empty;

  [JsonPropertyName("connected")]
  public bool Connected { get; set; }
}
