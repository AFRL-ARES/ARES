namespace HerkulexDRS.Responses.Parsers;

internal class StatusResponseParser : ResponseParser<StatusResponse>
{
  private const int _responseLength = 7;
  private readonly byte[] _statusAcknowledge = new byte[] { 0xFF, 0xFF, 0x09, 0xFD, 0x47, 0xF2, 0x0C };

  protected override bool TryParseResponse(byte[] data, out StatusResponse? response)
  {
    if (data.Length < _responseLength)
    {
      response = null;
      return false;
    }

    response = new StatusResponse(0xFD);

    if (data.SequenceEqual(_statusAcknowledge))
      response.ServoHasError = false;

    else
      response.ServoHasError = true;

    return true;
  }
}
