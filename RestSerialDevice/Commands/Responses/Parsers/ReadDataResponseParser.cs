using Ares.Device.Serial.Commands;
using GenericSerialDevice.Commands.Responses.JsonResponses;
using System.Text;
using System.Text.Json;

namespace GenericSerialDevice.Commands.Responses.Parsers;

public class ReadDataResponseParser : SerialResponseParser<ReadDataResponse>
{
  public override bool TryParseResponse(byte[] buffer, out ReadDataResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var jsonResponse = JsonSerializer.Deserialize<ReadDataJsonResponse>(Encoding.Default.GetString(buffer));

    if(jsonResponse is null)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    var dictionary = new Dictionary<string, string>();
    foreach(var item in jsonResponse.Variables)
      dictionary.Add(item.Key, item.Value.ToString() ?? string.Empty);

    response = new ReadDataResponse(dictionary);
    dataToRemove = null;
    return true;
  }
}
