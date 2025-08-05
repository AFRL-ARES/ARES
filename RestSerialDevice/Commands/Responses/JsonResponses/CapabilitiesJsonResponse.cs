using RestSerialDevice.Commands.Responses.JsonResponses.Json;
using System.Text.Json.Serialization;

namespace RestSerialDevice.Commands.Responses.JsonResponses;

public class CapabilitiesJsonResponse
{
  [JsonPropertyName("deviceName")]
  public string DeviceName { get; set; } = string.Empty;

  [JsonPropertyName("firmwareVersion")]
  public string FirmwareVersion { get; set; } = string.Empty;

  [JsonPropertyName("capabilities")]
  public Capabilities Capabilities { get; set; } = new Capabilities();
}
