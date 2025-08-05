using Ares.Device.Serial.Commands;
using RestSerialDevice.Commands.Responses.JsonResponses;
using RestSerialDevice.Structure;
using System.Text;
using System.Text.Json;

namespace RestSerialDevice.Commands.Responses.Parsers;

public class DeviceServicesResponseParser : SerialResponseParser<GetDeviceCapabilitiesResponse>
{
  public override bool TryParseResponse(byte[] buffer, out GetDeviceCapabilitiesResponse? response,
    out ArraySegment<byte>? dataToRemove)
  {
    try
    {
      var bufferString = Encoding.ASCII.GetString(buffer);
      var reader = new Utf8JsonReader(buffer);
      if (JsonDocument.TryParseValue(ref reader, out var doc))
      {
        var parsed = bufferString.Substring(0, (int)reader.BytesConsumed);
        dataToRemove = new ArraySegment<byte>(buffer, 0, (int)reader.BytesConsumed);
        var jsonResponse = JsonSerializer.Deserialize<CapabilitiesJsonResponse>(parsed);
        if (jsonResponse is null)
        {
          response = null;
          dataToRemove = null;
          return false;
        }

        var variables = jsonResponse.Capabilities.Variables.ConvertFromJsonVariables();
        var methods = jsonResponse.Capabilities.Functions.ConvertFromJsonMethods();

        response = new GetDeviceCapabilitiesResponse(jsonResponse.DeviceName, jsonResponse.FirmwareVersion, variables,
          methods);
        return true;
      }
    }
    catch (Exception e)
    {
      response = null;
      dataToRemove = null;
      return false;
    }
    
    response = null;
    dataToRemove = null;
    return false;
  }
}
