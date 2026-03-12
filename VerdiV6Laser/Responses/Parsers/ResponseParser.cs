using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace VerdiV6Laser.Responses.Parsers;

internal abstract class ResponseParser<TResponse> : SerialResponseParser<TResponse> where TResponse : SerialResponse
{
  private bool TryParseResponse(string[] bufferLines, out TResponse? response, out int parsedLineIndex)
  {
    for (var i = 0; i < bufferLines.Length; i++)
    {
      if (TryParseResponse(bufferLines[i], out response))
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
    var asciiBufferProxy = Encoding.ASCII.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith("\r\n");
    var availableLines = asciiBufferProxy.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    
    // If it doesn't end with \r\n, the last line might be incomplete
    if (!useLast && availableLines.Length > 0)
    {
        availableLines = availableLines.SkipLast(1).ToArray();
    }

    if (!TryParseResponse(availableLines, out response, out var parsedLineIndex))
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    // Calculate how many bytes to remove. 
    // We remove everything up to and including the line we parsed.
    var skippedBytes = 0;
    for (int i = 0; i <= parsedLineIndex; i++)
    {
        skippedBytes += Encoding.ASCII.GetByteCount(availableLines[i]) + 2; // +2 for \r\n
    }

    dataToRemove = new ArraySegment<byte>(buffer, 0, skippedBytes);
    return true;
  }
}
