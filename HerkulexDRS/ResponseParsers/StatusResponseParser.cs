using Ares.Device.Serial.Commands;
using HerkulexDRS.Responses;

namespace HerkulexDRS.ResponseParsers;

internal class StatusResponseParser : SerialResponseParser<StatusResponse>
{
  private const int statusByteIndex = 7;
  private const int _responseLength = 17;
  public override bool TryParseResponse(byte[] buffer, out StatusResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var bufferArray = buffer.ToArray();

    if (bufferArray.Length < _responseLength)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    if (bufferArray[statusByteIndex] == 0x00)
    {
      response = new StatusResponse();
    }





    throw new NotImplementedException();
  }
}
