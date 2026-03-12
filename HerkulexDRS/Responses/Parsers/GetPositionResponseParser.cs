using Ares.Toolkit.Serial.Commands;

namespace HerkulexDRS.Responses.Parsers;
public class GetPositionResponseParser : SerialResponseParser<GetPositionResponse>
{
  public override bool TryParseResponse(byte[] buffer, out GetPositionResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    if (buffer.Length == 0)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    // TODO: Implement actual protocol parsing
    response = new GetPositionResponse { Position = 0 };
    dataToRemove = new ArraySegment<byte>(buffer, 0, buffer.Length);
    return true;
  }
}
