using Ares.Device.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Commands.Responses.Parsers;

internal abstract class ResponseParser<TResponse> : SerialResponseParser<TResponse> where TResponse : CommandResponse
{
  private bool TryParseResponse(string[] bufferLines, out TResponse response, out int parsedLineIndex)
  {
    for(var i = 0; i < bufferLines.Length; i++)
    {
      if(bufferLines[i].StartsWith("\r"))
      {

      }
      if(TryParseResponse(bufferLines[i], out response))
      {
        parsedLineIndex = i;
        return true;
      }
    }

    parsedLineIndex = -1;
    response = null;
    return false;
  }

  protected abstract bool TryParseResponse(string line, out TResponse? response);

  public override bool TryParseResponse(byte[] buffer, out TResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var asciiBufferProxy = Encoding.UTF8.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith('\r');
    var availableLines = asciiBufferProxy.Split('\r', StringSplitOptions.RemoveEmptyEntries)[..^(useLast ? 0 : 1)];

    try
    {
      if(!TryParseResponse(availableLines, out response, out var parsedLineIndex))
      {
        response = null;
        dataToRemove = null;
        return false;
      }

      var skippedBytes = availableLines[..parsedLineIndex].Sum(s => s.Length + 1);// add 1 for the \r in the buffer

      var processedSize = availableLines[parsedLineIndex].Length + 1;// add 1 for the \r in the buffer

      dataToRemove = new ArraySegment<byte>(buffer, skippedBytes, processedSize);
      return true;
    }
    catch
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }
}
