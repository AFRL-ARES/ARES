using Ares.Device.Serial.Commands;
using GenericSerialDevice.Commands.Responses.JsonResponses;
using System.Text;
using System.Text.Json;

namespace GenericSerialDevice.Commands.Responses.Parsers;

public class DeviceInfoResponseParser : SerialResponseParser<GetDeviceInfoResponse>
{
  public override bool TryParseResponse(byte[] buffer, out GetDeviceInfoResponse? response,
    out ArraySegment<byte>? dataToRemove)
  {
    try
    {
      var funny = Encoding.ASCII.GetString(buffer);
      var jsonResponse = JsonSerializer.Deserialize<GetDeviceInfoJsonResponse>(funny);

      if (jsonResponse is null)
      {
        response = null;
        dataToRemove = null;
        return false;
      }

      response = new GetDeviceInfoResponse(jsonResponse.DeviceId, jsonResponse.DeviceName, jsonResponse.Hardware,
        jsonResponse.Connected);
      dataToRemove = null;
      return true;
    }
    catch (Exception e)
    {

      Console.WriteLine(":)");
    }
    finally
    {
      response = null;
      dataToRemove = null;
    }

    return false;
  }
}
