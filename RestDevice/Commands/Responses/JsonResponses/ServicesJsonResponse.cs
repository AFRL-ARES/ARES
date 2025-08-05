using RestDevice.Commands.Responses.JsonStructures;
using System.Text.Json.Serialization;

namespace RestDevice.Commands.Responses.JsonResponses;

public class ServicesJsonResponse
{
  [JsonPropertyName("deviceName")]
  public string DeviceName { get; set; } = string.Empty;

  [JsonPropertyName("firmwareVersion")]
  public string FirmwareVersion { get; set; } = string.Empty;

  [JsonPropertyName("capabilities")]
  public Capabilities Capabilities { get; set; } = new Capabilities();
}
