using Ares.Device.Serial.Commands;
using HerkulexDRS.Responses;

namespace HerkulexDRS.ResponseParsers;
public class GetPositionResponseParser : SerialResponseParser<GetPositionResponse>
{
  public override bool TryParseResponse(byte[] buffer, out GetPositionResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var bufferArray = buffer.ToArray();
    if (bufferArray.Length == 0)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    response = null;
    dataToRemove = null;
    return true;
  }
}
